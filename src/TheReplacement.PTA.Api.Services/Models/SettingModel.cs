using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents an encounter during a PTA session
    /// </summary>
    public class SettingModel : IDocument
    {
        /// <inheritdoc/>
        public ObjectId _id { get; set; }

        /// <summary>
        /// The encounter's id
        /// </summary>
        public Guid SettingId { get; set; }

        /// <summary>
        /// The game id associated with the encounter
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        /// The encounter's friendly nickname
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether or not the encounter is active for all trainers
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The type of Setting 
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The settings participants
        /// </summary>
        public IEnumerable<SettingParticipantModel> ActiveParticipants { get; set; }

        /// <summary>
        /// The encounter's environment
        /// </summary>
        public string[] Environment { get; set; }


        /// <summary>
        /// The encounter's shops
        /// </summary>
        public IEnumerable<Guid> Shops { get; set; }
    }
}
