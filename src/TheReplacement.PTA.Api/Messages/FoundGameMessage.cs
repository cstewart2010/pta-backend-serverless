using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;

namespace TheReplacement.PTA.Api.Messages
{
    public class FoundGameMessage : AbstractMessage
    {
        internal FoundGameMessage(Guid gameId)
        {
            Message = "Game was found";
            GameId = gameId;
            Trainers = DatabaseUtility.FindTrainersByGameId(gameId).Select(trainer => new PublicTrainer(trainer));
        }
        internal FoundGameMessage(Guid gameId, string nickname)
        {
            Message = "Game was found";
            GameId = gameId;
            Nickname = nickname;
            Trainers = DatabaseUtility.FindTrainersByGameId(gameId).Select(trainer => new PublicTrainer(trainer));
        }

        public string Nickname { get; }
        public override string Message { get; }
        public Guid GameId { get; }
        public IEnumerable<PublicTrainer> Trainers { get; }
    }
}
