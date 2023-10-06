namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a Pokemon Tabletop Adventures item
    /// </summary>
    public class ItemModel
    {
        /// <summary>
        /// The name of the item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The item's effects, if any
        /// </summary>
        public string Effects { get; set; }

        /// <summary>
        /// The Amount of that item that the user is holding, greater than 0
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// The type of item it is
        /// </summary>
        public string Type { get; set; }
    }
}
