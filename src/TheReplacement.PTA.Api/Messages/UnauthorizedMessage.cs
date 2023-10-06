using System;

namespace TheReplacement.PTA.Api.Messages
{
    public class UnauthorizedMessage : AbstractMessage
    {
        internal UnauthorizedMessage(Guid gameId)
        {
            Message = "Could not login in to game with provided password";
            GameId = gameId;
        }

        public override string Message { get; }
        public Guid GameId { get; }
    }
}
