using System.Collections.Generic;
using TheReplacement.PTA.Api.Objects;

namespace TheReplacement.PTA.Api.Models
{
    public class UserPageModel
    {
        public PageDataModel Previous { get; init; }
        public PageDataModel Next { get; init; }
        public IEnumerable<PublicUser> Users { get; init; }
    }
}
