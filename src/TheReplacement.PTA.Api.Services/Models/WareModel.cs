namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents an item sold at a shop
    /// </summary>
    public class WareModel
    {
        /// <summary>
        /// The item's cost
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// The effects of using the item
        /// </summary>
        public string Effects { get; set; }

        /// <summary>
        /// The type of item it is
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The amount of item on sale
        /// </summary>
        public int Quantity { get; set; }
    }
}
