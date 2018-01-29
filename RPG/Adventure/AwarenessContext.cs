using System;

namespace StreamRC.RPG.Adventure {
    public class AwarenessContext {
        public int AFKIndications { get; set; }
        public DateTime LastTrigger { get; set; }
        public int MaxDamage { get; set; }

        /// <summary>
        /// level of awareness skill
        /// </summary>
        public int Level { get; set; }
    }
}