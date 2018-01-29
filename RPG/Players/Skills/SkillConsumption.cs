using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.RPG.Players.Skills {

    /// <summary>
    /// list of learned skills and their skillpoint consumption
    /// </summary>
    [View("StreamRC.RPG.Players.Skills.skillconsumption.sql")]
    public class SkillConsumption : PlayerSkill {

        /// <summary>
        /// number of skillpoints consumed by skills
        /// </summary>
        public int Consumption { get; set; }
    }
}