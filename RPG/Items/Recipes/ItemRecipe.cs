namespace StreamRC.RPG.Items.Recipes {

    /// <summary>
    /// recipe for an item
    /// </summary>
    public class ItemRecipe {

        /// <summary>
        /// resulting item
        /// </summary>
        public long ItemID { get; set; }

        /// <summary>
        /// resources needed to create item
        /// </summary>
        public RecipeIngredient[] Ingredients { get; set; }
    }
}