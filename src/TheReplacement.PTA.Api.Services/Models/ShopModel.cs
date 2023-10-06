using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a shop in a PTA session
    /// </summary>
    public class ShopModel : IDocument
    {
        /// <inheritdoc/>
        public ObjectId _id { get; set; }

        /// <summary>
        /// The shop's id
        /// </summary>
        public Guid ShopId { get; set; }

        /// <summary>
        /// The game's id
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        /// The shop's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether the shop is active for trainers to visit
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// A collection of items on sale
        /// </summary>
        public Dictionary<string, WareModel> Inventory { get; set; }
    }
}
