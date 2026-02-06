using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Core.Utils.Logging;
using Eco.Gameplay.Aliases;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Items;
using Eco.Gameplay.Items.Recipes;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Settlements.ClaimStakes;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Messaging.Notifications;
using Eco.Mods.TechTree;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using Eco.Shared.Utils;
using Eco.Simulation.Time;
using Eco.WorldGenerator;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace KnownToAll
{
    public class KnownToAllMeta : IModInit
    {
        public static ModRegistration Register() => new()
        {
            ModName = "KnownToAll",
            ModDescription = Localizer.DoStr($"When a skill book is crafted, everyone learns the skill."),
            ModDisplayName = "Known To All"
        };
    }

    [SupportedOSPlatform("windows7.0")]
    [Priority(PriorityAttribute.VeryLow)] // Making this one of the last mods to load. If NoMoreBooks is enabled we want to load after it to warn about book-less recipes.
    public class KnownToAll : IModKitPlugin, IGameActionAware, IInitializablePlugin, IShutdownablePlugin
    {
        public const string VERSION = "v0.1";

        public string GetCategory() => "Mods";
        public string GetStatus() => Localizer.DoStr($"Status: {_status}. Version: {VERSION}");
        private string _status = "Starting...";

        public void ActionPerformed(GameAction action)
        {
            if (action is ItemCraftedAction itemCrafted &&
                itemCrafted.ItemUsed is SkillBook skillBook)
            {
                HandleItemCrafted(itemCrafted.Citizen, skillBook);
            }
            if (action is FirstLogin firstLogin)
            {
                HandleFirstLogin(firstLogin.Citizen);
            }
        }

        public LazyResult ShouldOverrideAuth(IAlias? alias, IOwned? property, GameAction? action)
        {
            return LazyResult.FailedNoMessage;
        }
        public void Initialize(TimedTask timer)
        {
            ActionUtil.AddListener(this);
            var noBookSkills = Skill.AllSkills.Where(x =>
                x.RootSkillTree != x.SkillTree &&
                x.SkillTree.RequiresScroll &&
                !x.SkillTree.IsUsingSkillBook
            );
            foreach (var skill in noBookSkills) 
            {
                Logger.Info($"This mod cannot work with the '{skill.Name}' skill. It doesn't have a recipe that produces a skill book.");
            }
            Logger.Info($"Mod Version: {VERSION}");
            _status = "Ok";
        }

        public Task ShutdownAsync()
        {
            ActionUtil.RemoveListener(this);
            _status = "Shutdown";
            return Task.CompletedTask;
        }

        private void HandleFirstLogin(User user)
        {
            var discoveredSkills = Skill.AllSkills.Where(x => x.IsDiscovered());
            foreach (var skill in discoveredSkills) {
                user.Skillset.LearnSkill(skill.Type);
            }
        }

        private void HandleItemCrafted(User crafter, SkillBook book)
        {
            if (!book.Skill.IsDiscovered())
            {
                AnnounceDiscovery(book.Skill, crafter);
                EmulateSkillScroll(crafter, book);
                foreach (var user in UserManager.Users)
                {
                    if (user == crafter) continue;
                    EmulateSkillScroll(user, book, notify: false);
                }
            }
        }

        private void EmulateSkillScroll(User user, SkillBook book, bool notify = true)
        {
            // Give the user their stakes and papers
            #region SkillUtils.cs:19-31
            if (user.Skillset.HasSkill(book.SkillType))
            {
                Logger.Debug($"EmulateSkillScroll failed: User {user.MarkedUpName} tried to learn {book.Skill.MarkedUpName} but failed. Did they already know it?");
                return;
            }
            var stakesPerScroll = DifficultySettingsConfig.Advanced.ClaimStakesGrantedUponSkillscrollConsumed;
            int numStakes = AmountToSpawn(user, stakesPerScroll);
            if (numStakes > 0)
            {
                InventoryUtils.AddItemsWithVoidStorageFallback(
                    Localizer.Do($"Rewards for learning {book.Skill.MarkedUpName}"),
                    user,
                    Item.Get(typeof(OutpostClaimStakeItem)),
                    numStakes
                );
            }
            var papersPerScroll = DifficultySettingsConfig.Advanced.ClaimPapersGrantedUponSkillscrollConsumed;
            int numPapers = AmountToSpawn(user, papersPerScroll);
            if (numPapers > 0)
            {
                InventoryUtils.AddItemsWithVoidStorageFallback(
                    Localizer.Do($"Rewards for learning {book.Skill.MarkedUpName}"),
                    user,
                    Item.Get(typeof(ClaimPaperItem)),
                    numPapers
                );
            }
            #endregion
            if (notify)
                user.Skillset.LearnSkillAndNotify(book.Skill);
            else
                user.Skillset.LearnSkill(book.SkillType);
            // Housekeeping and messaging the user about stakes and papers gained
            #region SkillUtils.cs:35-36
            user.Skillset.ScrollsRead++;
            if (numPapers > 0 || numStakes > 0)
                user.MsgLoc($"You've earned {Item.Get(typeof(OutpostClaimStakeItem)).UILinkAndNumber(stakesPerScroll)} and {Item.Get(typeof(ClaimPaperItem)).UILinkAndNumber(papersPerScroll)}.");
            #endregion
        }
    
        private int AmountToSpawn(User user, float amountPerScroll)
        {
            // Determines when a user can earn a whole stake or paper from fractional amounts.
            // E.g. Players earn half a stake/paper per scroll read. They'll get one stake/paper when they learn the 2nd skill and no stake/paper on the 1st.
            #region SkillUtils:41-47
            var prevStakes = (int)(amountPerScroll * user.Skillset.ScrollsRead);
            var curStakes = (int)(amountPerScroll * (user.Skillset.ScrollsRead + 1));
            var toAddStakes = curStakes - prevStakes;
            return toAddStakes;
            #endregion
        }

        private void AnnounceDiscovery(Skill skill, User creator)
        {
            // This is the first time the world has seen this skill. Broadcast the discovery.
            #region SkillBook.cs:37-38
            skill.SkillTree.Parent.StaticSkill.SkillTree.TryDiscover(WorldTime.Seconds, creator); //Trigger a discover for the root skill, in case the user gave themselves this book with a /give command to test discovery.
            skill.SkillTree.TryDiscover(WorldTime.Seconds, creator); //Trigger a discover, in case the user gave themselves this book with a /give command to test discovery.
            #endregion 
            var msg = $"{creator.MarkedUpName} completed their research! {DemographicManager.Everyone.MarkedUpName} has learned the specialty {skill.MarkedUpName}.";
            Logger.Info(msg);
            NotificationManager.ServerMessageToAll(Localizer.DoStr(msg));
            foreach (var user in UserManager.Users)
            {
                if (user == creator) continue;
                user.MsgOrMailLoc(
                    $"{msg}",
                    NotificationCategory.Notifications,
                    NotificationStyle.Mail
                );
            }
        }
    }
}
