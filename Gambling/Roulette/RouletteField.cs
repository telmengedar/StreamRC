namespace StreamRC.Gambling.Roulette {

    /// <summary>
    /// field on roulette board
    /// </summary>
    public class RouletteField {

        /// <summary>
        /// creates a new <see cref="RouletteField"/>
        /// </summary>
        /// <param name="number"></param>
        /// <param name="color"></param>
        public RouletteField(int number, RouletteColor color) {
            Number = number;
            Color = color;
        }

        /// <summary>
        /// value of field
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// color of field
        /// </summary>
        public RouletteColor Color { get; set; }

        public override string ToString() {
            return $"{Number}{Color.ToString()[0]}";
        }
    }
}