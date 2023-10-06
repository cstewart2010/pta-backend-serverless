namespace TheReplacement.PTA.Api.Services.Interfaces
{
    /// <summary>
    /// Provides a Name property for DexDocuments
    /// </summary>
    public interface IDexDocument
    {
        /// <summary>
        /// The name of the dex document
        /// </summary>
        public string Name { get; set; }
    }
}
