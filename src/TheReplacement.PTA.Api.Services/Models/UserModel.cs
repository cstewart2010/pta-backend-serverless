using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a User in Pokemon Tabletop Adventures 
    /// </summary>
    public class UserModel : IAuthenticated, IDocument
    {
        /// <summary>
        /// The default constructor for the MongoDB Csharp Driver
        /// </summary>
        public UserModel() { }

        /// <summary>
        /// Initializes a new instance of <see cref="UserModel"/>
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public UserModel(string username, string password)
        {
            UserId = Guid.NewGuid();
            Username = username;
            PasswordHash = EncryptionUtility.HashSecret(password);
            IsOnline = true;
            ActivityToken = EncryptionUtility.GenerateToken();
            DateCreated = DateTime.UtcNow.ToString("u");
            SiteRole = UserRoleOnSite.Active.ToString();
            Games = Array.Empty<Guid>();
            Messages = Array.Empty<Guid>();
        }

        /// <inheritdoc />
        public ObjectId _id { get; set; }

        /// <summary>
        /// Id for PTA user
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Username for PTA user
        /// </summary>
        public string Username { get; set; }

        /// <inheritdoc />
        public bool IsOnline { get; set; }

        /// <inheritdoc />
        public string PasswordHash { get; set; }

        /// <summary>
        /// The 30 minute activity token for trainers
        /// </summary>
        public string ActivityToken { get; set; }

        /// <summary>
        /// Date PTA user account was created
        /// </summary>
        public string DateCreated { get; set; }

        /// <summary>
        /// Site role for PTA user
        /// </summary>
        public string SiteRole { get; set; }

        /// <summary>
        /// Games of which the PTA user is a member
        /// </summary>
        public ICollection<Guid> Games { get; set; }

        /// <summary>
        /// List of PTA user's messages
        /// </summary>
        public IEnumerable<Guid> Messages { get; set; }
    }
}
