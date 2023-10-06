namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a message in Pokemon Tabletop Adventures 
    /// </summary>
    public class UserMessageModel
    {
        /// <summary>
        /// The default constructor for the MongoDb Csharp Driver
        /// </summary>
        public UserMessageModel() { }

        /// <summary>
        /// Initializes a new instance of <see cref="UserMessageModel"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="messageContent"></param>
        public UserMessageModel(Guid userId, string messageContent)
        {
            Timestamp = DateTime.UtcNow.ToString();
            Message = messageContent;
            User = userId;
        }

        /// <summary>
        /// User that sent the message
        /// </summary>
        public Guid User { get; set; }

        /// <summary>
        /// Contents of what was sent
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp for when the message was created
        /// </summary>
        public string Timestamp { get; set; }
    }
}
