namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Stats values for Pokemon and Trainers
    /// </summary>
    public class StatsModel
    {
        /// <summary>
        /// The HP stat
        /// </summary>
        public int HP { get; set; }

        /// <summary>
        /// The Attack stat
        /// </summary>
        public int Attack { get; set; }

        /// <summary>
        /// The Defense stat
        /// </summary>
        public int Defense { get; set; }

        /// <summary>
        /// The Special Attack stat
        /// </summary>
        public int SpecialAttack { get; set; }

        /// <summary>
        /// The Special Defense stat
        /// </summary>
        public int SpecialDefense { get; set; }

        /// <summary>
        /// The Speed stat
        /// </summary>
        public int Speed { get; set; }
    }
}
