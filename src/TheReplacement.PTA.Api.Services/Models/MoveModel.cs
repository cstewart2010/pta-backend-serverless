using MongoDB.Bson;
using System.Collections.Generic;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents an move in the MoveDex
    /// </summary>
    public class MoveModel : IDocument, IDexDocument
    {
        /// <inheritdoc/>
        public ObjectId _id { get; set; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <summary>
        /// The range of the move
        /// </summary>
        public string Range { get; set; }

        /// <summary>
        /// The move's <see cref="Services.Enums.PokemonTypes"/>
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The pokemon's stats
        /// </summary>
        public string Stat { get; set; }

        /// <summary>
        /// The frequency at which the move can be used
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// The damage rolls, if applicable
        /// </summary>
        public string DiceRoll { get; set; }

        /// <summary>
        /// The effects of using the move
        /// </summary>
        public string Effects { get; set; }

        /// <summary>
        /// The skills that the mvoe grants, if any
        /// </summary>
        public IEnumerable<string> GrantedSkills { get; set; }

        /// <summary>
        /// The move's Contest stat, if any
        /// </summary>
        public string ContestStat { get; set; }

        /// <summary>
        /// The move's Contest keyword, if any
        /// </summary>
        public string ContestKeyword { get; set; }
    }
}
