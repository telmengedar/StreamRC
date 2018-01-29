
namespace StreamRC.RPG.Effects {

    /// <summary>
    /// effect which wears off eventually
    /// </summary>
    public interface ITemporaryEffect {

        /// <summary>
        /// name of effect
        /// </summary>
        string Name { get; }

        /// <summary>
        /// initializes the effect
        /// </summary>
        void Initialize();

        /// <summary>
        /// called before effect wears off
        /// </summary>
        void WearOff();

        /// <summary>
        /// time until effect wears off
        /// </summary>
        double Time { get; set; }

        /// <summary>
        /// effect level
        /// </summary>
        int Level { get; set; }
    }
}