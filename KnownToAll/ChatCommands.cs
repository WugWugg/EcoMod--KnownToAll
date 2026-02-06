using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Gameplay.Systems.Messaging.Notifications;
using Eco.Shared.Localization;
using Eco.Shared.Math;
using Eco.Shared.Services;
using Eco.WorldGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace KnownToAll
{
    [ChatCommandHandler]
    [SupportedOSPlatform("windows7.0")]
    public static class KnownToAllCommands
    {
        [ChatSubCommand("knowntoall", "Debug Testing", ChatAuthorizationLevel.User)]
        public static void wugtest(User user)
        {
            var skill = Skill.AllSkills.First();
            var msg = $"{user.MarkedUpName} completed their research! {DemographicManager.Everyone.MarkedUpName} has learned the specialty {skill.MarkedUpName}.";
            Logger.Info(msg);
            NotificationManager.ServerMessageToAll(Localizer.DoStr(msg));
            
            user.MsgOrMailLoc(
                $"{msg}",
                NotificationCategory.Notifications,
                NotificationStyle.Mail
            );
        }

        [ChatCommand("")]
        public static void knowntoall() { }

        [ChatSubCommand("knowntoall", "Prints the version you're using.", ChatAuthorizationLevel.Admin)]
        public static void version(IChatClient chat)
        {
            chat.MsgLoc($"Version: {KnownToAll.VERSION}");
        }

        [ChatSubCommand("knowntoall", "Pretty prints a users skill levels.", "listSkills", ChatAuthorizationLevel.Admin)]
        public static void listSkills(User chat, string username)
        {
            var user = UserManager.FindUserByName(username);
            if (user == null)
            {
                chat.MsgLoc($"Command Failed. Couldn't find user with name '{username}'");
                return;
            }
            var content = new LocStringBuilder();
            var userSkills = user.Skillset.Skills.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var skill in userSkills)
            {
                content.AppendLineLocStr($"<align=left>{skill.Level}\t{skill.MarkedUpName}</align>");
            }
            var missingSkills = Skill.AllSkills.ExceptBy(userSkills.Select(x => x.Type), x => x.Type);
            if (missingSkills.Any()) content.AppendLineLocStr("<br><align=left>Unknown Skills:</align>");
            foreach (var skill in missingSkills)
            {
                content.AppendLineLocStr($"<align=left><color=red>x</color>\t{skill.MarkedUpName}</align>");
            }
            chat.Player.OpenInfoPanel(
                Localizer.DoStr($"Skill Summary for {user.MarkedUpName}"),
                content.ToLocString(),
                "admin"
            );
        }

        [ChatSubCommand("knowntoall", "Pretty prints a users skill levels.", "triggerLogin", ChatAuthorizationLevel.DevTier)]
        public static void triggerLogin(User chat, string username)
        {
            var user = UserManager.FindUserByName(username);
            if (user == null)
            {
                chat.MsgLoc($"Command Failed. Couldn't find user with name '{username}'");
                return;
            }
            var login = new FirstLogin { Citizen = user, ActionLocation = Vector3i.Zero }.TryPerform(chat);
        }
    }
}
