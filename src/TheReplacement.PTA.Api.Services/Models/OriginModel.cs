using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents an origin in the OriginDex
    /// </summary>
    public class OriginModel : IDocument, IDexDocument
    {
        /// <inheritdoc/>
        public ObjectId _id { get; set; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <summary>
        /// The Skill granted by the Origin
        /// </summary>
        public string Skill { get; set; }

        /// <summary>
        /// The lifestyle granted by the Origin
        /// </summary>
        public string Lifestyle { get; set; }

        /// <summary>
        /// The trainer's starting funds
        /// </summary>
        public int Savings { get; set; }

        /// <summary>
        /// The trainer's starting equipment
        /// </summary>
        public string Equipment { get; set; }

        /// <summary>
        /// The trainer's starting equipment
        /// </summary>
        public IEnumerable<StartingEquipment> StartingEquipmentList { get;set;}

        /// <summary>
        /// The trainer's starting pokemon, if applicable
        /// </summary>
        public string StartingPokemon { get; set; }

        /// <summary>
        /// The Origin's specialized feature
        /// </summary>
        public FeatureModel Feature { get; set; }
    }

    /// <summary>
    /// Container for starting Equipment
    /// </summary>
    public enum StartingEquipmentType
    {
        /// <summary>
        /// Errored
        /// </summary>
        None,

        /// <summary>
        /// Represents a Trainer Item
        /// </summary>
        Trainer,

        /// <summary>
        /// Represents a Pokeball
        /// </summary>
        Pokeball,

        /// <summary>
        /// Represents a Medical Item
        /// </summary>
        Medical,

        /// <summary>
        /// Represents a Berry
        /// </summary>
        Berry,

        /// <summary>
        /// Represents a Pokemon Item
        /// </summary>
        Pokemon
    }

    /// <summary>
    /// Represents a Starting Equipment Item
    /// </summary>
    public class StartingEquipment
    {
        /// <summary>
        /// The name of the item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of item
        /// </summary>
        public StartingEquipmentType Type { get; set; }

        /// <summary>
        /// The Amount of item to start with.
        /// </summary>
        public int Amount { get; set; }
    }
}
