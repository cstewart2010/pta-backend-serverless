using System.Collections.Generic;

namespace TheReplacement.PTA.Api.Models
{
    internal class NewNpcDataModel
    {
        public string TrainerName { get; init; }
        public IEnumerable<string> Feats { get; init; }
        public IEnumerable<string> Classes { get; init; }
    }
}
