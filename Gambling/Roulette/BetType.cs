namespace StreamRC.Gambling.Roulette {
    public enum BetType {

        /// <summary>
        /// bet on a specific field
        /// </summary>
         Plein,

         Cheval,
         TransversalePlein,
         Carre,
         LesQuatre,
         LesCinq,
         TransversaleSimple,
         Douzaines,
         Colonnes,

         /// <summary>
         /// bet on red or black
         /// </summary>
         Color,

         /// <summary>
         /// bet on odd or even number
         /// </summary>
         OddEven,

         /// <summary>
         /// bet on first half or second half of number != 0
         /// </summary>
         HalfBoard,

    }
}