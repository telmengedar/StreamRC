namespace StreamRC.RPG.Items.Recipes {

    /// <summary>
    /// ingredient specified in recipe resource
    /// </summary>
    public class ResourceIngredient {

        /// <summary>
        /// name of item
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// determines whether item is consumed
        /// </summary>
        public bool Consumed { get; set; }
    }
}