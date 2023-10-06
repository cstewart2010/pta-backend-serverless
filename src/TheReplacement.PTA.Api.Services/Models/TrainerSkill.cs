namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a container for data regarding a particular trainer skill
    /// </summary>
    public class TrainerSkill
    {
        /// <summary>
        /// The skill name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this skill has been boosted by a talent
        /// </summary>
        public bool Talent1 { get; set; }

        /// <summary>
        /// Whether this skill has been boosted by a second talent
        /// </summary>
        public bool Talent2 { get; set; }

        /// <summary>
        /// The stat that this skill targets
        /// </summary>
        public string ModifierStat { get; set; }
    }
}
