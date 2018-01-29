namespace StreamRC.RPG.Items.Recipes {

    /// <summary>
    /// recipe for an item
    /// </summary>
    public class ResourceRecipe {

        /// <summary>
        /// resulting item
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// items needed to create item
        /// </summary>
        public ResourceIngredient[] Ingredients { get; set; }
    }
}