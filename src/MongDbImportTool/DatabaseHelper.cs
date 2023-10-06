using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace MongoDbImportTool
{
    internal static class DatabaseHelper
    {
        public static void AddDocuments<TDocument>(
            string collectionName,
            IEnumerable<TDocument> documents) where TDocument : IDexDocument
        {
            DexUtility.AddDexEntries(collectionName, documents, Console.Out);
        }
    }
}
