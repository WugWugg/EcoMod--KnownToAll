using Eco.Gameplay.Items.Recipes;
namespace Eco.Mods.TechTree
{

    public partial class ButcherySkillBookRecipe
    {
        partial void ModsPreInitialize()
        {
            this.CraftMinutes = RecipeFamily.CreateCraftTimeValue(0.083f); // 5 seconds
        }
    }

    public partial class FarmingSkillBookRecipe
    {
        partial void ModsPreInitialize()
        {
            this.CraftMinutes = RecipeFamily.CreateCraftTimeValue(0.083f); // 5 seconds
        }
    }

    public partial class MasonrySkillBookRecipe
    {
        partial void ModsPreInitialize()
        {
            this.CraftMinutes = RecipeFamily.CreateCraftTimeValue(0.083f); // 5 seconds
        }
    }
}