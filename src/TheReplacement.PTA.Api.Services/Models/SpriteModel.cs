using MongoDB.Bson;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a sprite in the Pokemon Tabletop adventures app
    /// </summary>
    public class SpriteModel
    {
        /// <summary>
        /// MongoDB id
        /// </summary>
        public ObjectId _id { get; set; }

        /// <summary>
        /// Friendly text for the select
        /// </summary>
        public string FriendlyText { get; set; }

        /// <summary>
        /// Value for the Pokemon Showdown sprite
        /// </summary>
        public string Value { get; set; }
    }
}
