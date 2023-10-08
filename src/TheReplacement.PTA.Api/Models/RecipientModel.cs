using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Models
{
    internal class RecipientModel
    {
        public PublicTrainer Trainer { get; init; }
        public ShopModel Shop { get; init; }
    }
}
