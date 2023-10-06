using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents an NPC in Pokemon Tabletop Adventures
    /// </summary>
    public class NpcModel : IPerson, IDocument
    {

        private static readonly IReadOnlyDictionary<string, string> TrainerSkillNames = new Dictionary<string, string>
        {
            {"Acrobatics", "speed"},
            {"Athletics", "attack"},
            {"Bluff/Deception", "specialDefense"},
            {"Concentration", "defense"},
            {"Constitution", "defense"},
            {"Diplomacy/Persuasion", "specialDefense"},
            {"Engineering/Operation", "specialAttack"},
            {"History", "specialAttack"},
            {"Insight", "specialDefense"},
            {"Investigation", "specialAttack"},
            {"Medicine", "specialAttack"},
            {"Nature", "specialAttack"},
            {"Perception", "specialDefense"},
            {"Performance", "specialDefense"},
            {"Pokémon Handling", "specialDefense"},
            {"Programming", "specialAttack"},
            {"Sleight of Hand", "speed"},
            {"Stealth", "speed"}
        };

        /// <summary>
        /// Initializes a new instance of <see cref="NpcModel"/> with default values
        /// </summary>
        public NpcModel()
        {
            TrainerSkills = TrainerSkillNames.Select(skill => new TrainerSkill { Name = skill.Key, ModifierStat = skill.Value });
            Gender = "Agender";
            Description = string.Empty;
            Personality = string.Empty;
            Background = string.Empty;
            Goals = string.Empty;
            Species = string.Empty;
        }
        /// <inheritdoc />
        public ObjectId _id { get; set; }

        /// <summary>
        /// The NPC's unique id
        /// </summary>
        public Guid NPCId { get; set; }

        /// <summary>
        /// The trainer's name
        /// </summary>
        public string TrainerName { get; set; }

        /// <summary>
        /// The trainer's classes
        /// </summary>
        public IEnumerable<string> TrainerClasses { get; set; }

        /// <summary>
        /// The trainer's stats
        /// </summary>
        public StatsModel TrainerStats { get; set; }

        /// <summary>
        /// The npc's current hp
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// The trainer's Feats
        /// </summary>
        public IEnumerable<string> Feats { get; set; }

        /// <summary>
        /// The PTA game session id
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        /// The trainer's level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The trainer's skills
        /// </summary>
        public IEnumerable<TrainerSkill> TrainerSkills { get; set; }

        /// <summary>
        /// The trainer's age
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// The trainer's gender
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The trainer's height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The trainer's weight
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// A summary of the trainer's description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A summary of the trainer's personality
        /// </summary>
        public string Personality { get; set; }

        /// <summary>
        /// A summary of the trainer's background
        /// </summary>
        public string Background { get; set; }

        /// <summary>
        /// A summary of the trainer's goals
        /// </summary>
        public string Goals { get; set; }

        /// <summary>
        /// The trainer's species (human or a pokemon)
        /// </summary>
        public string Species { get; set; }

        /// <summary>
        /// The npc's sprite
        /// </summary>
        public string Sprite { get; set; }
    }
}
