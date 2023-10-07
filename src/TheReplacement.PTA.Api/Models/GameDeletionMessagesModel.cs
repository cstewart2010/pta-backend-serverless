using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Messages;

namespace TheReplacement.PTA.Api.Models
{
    internal class GameDeletionMessagesModel
    {
        public IEnumerable<GenericMessage> PokemonDeletionResult { get; init; }
        public GenericMessage TrainerDeletionResult { get; init; }
        public GenericMessage GameDeletionResult { get; init; }
    }
}
