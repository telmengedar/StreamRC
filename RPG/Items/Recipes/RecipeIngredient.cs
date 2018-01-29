namespace StreamRC.RPG.Items.Recipes {

    /// <summary>
    /// ingredient of recipe
    /// </summary>
    public class RecipeIngredient {

        /// <summary>
        /// name of item
        /// </summary>
        public long Item { get; set; }

        /// <summary>
        /// determines whether item is consumed
        /// </summary>
        public bool Consumed { get; set; }
    }
}