using System;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Messages
{
    public class CreatedGameMessage : AbstractMessage
    {
        internal CreatedGameMessage(TrainerModel trainer)
        {
            Message = "Game was created";
            GameId = trainer.GameId;
            GameMaster = new PublicTrainer(trainer);
        }

        public override string Message { get; }
        public Guid GameId { get; }
        public PublicTrainer GameMaster { get; }
    }
}
