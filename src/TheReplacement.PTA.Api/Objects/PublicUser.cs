using System;
using System.Collections.Generic;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Objects
{
    public class PublicUser
    {
        internal PublicUser(UserModel user)
        {
            UserId = user.UserId;
            Username = user.Username;
            DateCreated = user.DateCreated;
            Games = user.Games;
            Messages = user.Messages;
        }

        /// <summary>
        /// Id for PTA user
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Username for PTA user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Date PTA user account was created
        /// </summary>
        public string DateCreated { get; set; }

        /// <summary>
        /// Games of which the PTA user is a member
        /// </summary>
        public IEnumerable<Guid> Games { get; set; }

        /// <summary>
        /// List of PTA user's messages
        /// </summary>
        public IEnumerable<Guid> Messages { get; set; }
    }
}
