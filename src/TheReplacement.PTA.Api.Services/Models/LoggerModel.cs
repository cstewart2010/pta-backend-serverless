using MongoDB.Bson;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Services.Models
{
    internal class LoggerModel : IDocument
    {
        public ObjectId _id { get; set; }
        public string Message { get; set; }
        public string LogLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public string AffectedCollection { get; set; }
    }
}
