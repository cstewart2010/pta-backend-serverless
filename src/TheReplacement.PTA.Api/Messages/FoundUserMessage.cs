using System;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Messages
{
    public class FoundUserMessage : AbstractMessage
    {
        internal FoundUserMessage(UserModel user)
        {
            Message = "Trainer was found";
            User = new PublicUser(user);
            IsAdmin = Enum.TryParse<UserRoleOnSite>(user.SiteRole, out var result) && result == UserRoleOnSite.SiteAdmin;
        }

        public override string Message { get; }
        public PublicUser User { get; }
        public bool IsAdmin { get; }
    }

}
