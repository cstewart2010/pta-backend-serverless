using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Models
{
    internal class TradeResultModel
    {
        public PokemonModel LeftPokemon { get; init; }
        public PokemonModel RightPokemon { get; init; }
    }
}
