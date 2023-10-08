using System.Collections.Generic;

namespace TheReplacement.PTA.Api.Models
{
    internal class PokemonEvolutionDataModel
    {
        public string NextForm { get; init; }
        public IEnumerable<string> KeptMoves { get; init; }
        public IEnumerable<string> NewMoves { get; init; }
    }
}
