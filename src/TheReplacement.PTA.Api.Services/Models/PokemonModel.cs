using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a Pokemon in Pokemon Tabletop Adventures
    /// </summary>
    public class PokemonModel: IDocument
    {
        /// <inheritdoc />
        public ObjectId _id { get; set; }

        /// <summary>
        /// The Pokemon's unique id
        /// </summary>
        public Guid PokemonId { get; set; }

        /// <summary>
        /// The Pokemon's dex number
        /// </summary>
        public int DexNo { get; set; }

        /// <summary>
        /// The Pokemon species' form name
        /// </summary>
        public string Form { get; set; }

        /// <summary>
        /// The Pokemon species' alternate forms, if any
        /// </summary>
        public IEnumerable<string> AlternateForms { get; set; }

        /// <summary>
        /// The Pokemon species' normal image
        /// </summary>
        public string NormalPortrait { get; set; }

        /// <summary>
        /// The Pokemon species' shiny image
        /// </summary>
        public string ShinyPortrait { get; set; }

        /// <summary>
        /// The species name for the pokemon
        /// </summary>
        public string SpeciesName { get; set; }

        /// <summary>
        /// The trainer's unique id
        /// </summary>
        public Guid TrainerId { get; set; }

        /// <summary>
        /// The game's unique id
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        /// The pokemon's original trainer id
        /// </summary>
        public Guid OriginalTrainerId { get; set; }

        /// <summary>
        /// The Pokemon's gender
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The Pokemon's status
        /// </summary>
        public string PokemonStatus { get; set; }

        /// <summary>
        /// The Pokemon's nickname. Defaults to the Species name is nothing is selected
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// The Pokemon's moves
        /// </summary>
        public IEnumerable<string> Moves { get; set; }

        /// <summary>
        /// The Pokemon's type positioning
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The Pokemon's catchrate
        /// </summary>
        public int CatchRate { get; set; }

        /// <summary>
        /// The Pokemon's nature positioning
        /// </summary>
        public string Nature { get; set; }

        /// <summary>
        /// Whether the pokemon is shiny or not
        /// </summary>
        public bool IsShiny { get; set; }

        /// <summary>
        /// Whether the pokemon is on the team
        /// </summary>
        public bool IsOnActiveTeam { get; set; }

        /// <summary>
        /// Whether the pokemon is ready to evolve
        /// </summary>
        public bool CanEvolve { get; set; }

        /// <summary>
        /// Collection of Pokemon stats
        /// </summary>
        public StatsModel PokemonStats { get; set; }

        /// <summary>
        /// The pokemon's current pokeball
        /// </summary>
        public string Pokeball { get; set; }

        /// <summary>
        /// The pokemon'ss current hp
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// The Pokemon species' <see cref="Enums.Size"/>
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// The Pokemon species' <see cref="Enums.Weight"/>
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// The Pokemon species' Skills
        /// </summary>
        public IEnumerable<string> Skills { get; set; }

        /// <summary>
        /// The Pokemon's Passives
        /// </summary>
        public IEnumerable<string> Passives { get; set; }

        /// <summary>
        /// The Pokemon's <see cref="Enums.EggGroups"/>
        /// </summary>
        public IEnumerable<string> EggGroups { get; set; }

        /// <summary>
        /// The Pokemon's Proficiencies
        /// </summary>
        public IEnumerable<string> Proficiencies { get; set; }

        /// <summary>
        /// The Pokemon's hatch rate
        /// </summary>
        public string EggHatchRate { get; set; }

        /// <summary>
        /// The Pokemon's habitats
        /// </summary>
        public IEnumerable<string> Habitats { get; set; }

        /// <summary>
        /// The Pokemon's diet
        /// </summary>
        public string Diet { get; set; }

        /// <summary>
        /// The Pokemon's <see cref="Enums.Rarity"/>
        /// </summary>
        public string Rarity { get; set; }

        /// <summary>
        /// The Pokemon's GMax move, if any
        /// </summary>
        public string GMaxMove { get; set; }

        /// <summary>
        /// The Pokemon's previous evolution, if any
        /// </summary>
        public string EvolvedFrom { get; set; }

        /// <summary>
        /// The Pokemon's legendary stats, if applicable
        /// </summary>
        public LegendaryStatsModel LegendaryStats { get; set; }
    }
}
