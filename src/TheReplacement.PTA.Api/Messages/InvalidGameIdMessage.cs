using System;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Messages
{
    public class InvalidGameIdMessage : AbstractMessage
    {
        internal InvalidGameIdMessage(TrainerModel trainer)
        {
            Message = $"{trainer.TrainerId} had an invalid game id";
            GameId = trainer.GameId;
        }

        public override string Message { get; }
        public Guid GameId { get; }
    }
}
