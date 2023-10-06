using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Container for all possible SettingParticipantModel types
    /// </summary>
    public enum SettingParticipantType
    {
        /// <summary>
        /// Represents a <see cref="TrainerModel"/>
        /// </summary>
        Trainer = 1,

        /// <summary>
        /// Represents a <see cref="PokemonModel"/>
        /// </summary>
        Pokemon = 2,

        /// <summary>
        /// Represents an Enemy <see cref="NpcModel"/>
        /// </summary>
        EnemyNpc = 3,

        /// <summary>
        /// Represents a Enemy <see cref="PokemonModel"/>
        /// </summary>
        EnemyPokemon = 4,

        /// <summary>
        /// Represents an Neutral <see cref="NpcModel"/>
        /// </summary>
        NeutralNpc = 5,

        /// <summary>
        /// Represents a Neutral <see cref="PokemonModel"/>
        /// </summary>
        NeutralPokemon = 6,

        /// <summary>
        /// Represents a Shop
        /// </summary>
        Shop = 7
    }
}
