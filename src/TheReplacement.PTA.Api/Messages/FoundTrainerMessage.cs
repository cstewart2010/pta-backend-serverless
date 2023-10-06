using System;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;

namespace TheReplacement.PTA.Api.Messages
{
    public class FoundTrainerMessage : AbstractMessage
    {
        internal FoundTrainerMessage(Guid userId, Guid gameId)
        {
            var trainer = DatabaseUtility.FindTrainerById(userId, gameId);
            var user = DatabaseUtility.FindUserById(userId);
            Message = "Trainer was found";
            Trainer = new PublicTrainer(trainer);
            User = new PublicUser(user);
            GameNickname = DatabaseUtility.GetGameNickname(trainer.GameId);
        }

        public override string Message { get; }
        public PublicTrainer Trainer { get; }
        public PublicUser User { get; }
        public string GameNickname { get; set; }
    }
}
