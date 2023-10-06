using Newtonsoft.Json;

namespace TheReplacement.PTA.Api.Services
{
    /// <summary>
    /// Represents a container for a MongoWriteException
    /// </summary>
    public class MongoWriteError
    {
        internal MongoWriteError(string jsonString)
        {
            WriteErrorJsonString = jsonString;
        }

        /// <summary>
        /// Error from MongoWrite Exception as a json string
        /// </summary>
        public string WriteErrorJsonString { get; }

        internal object ToObject()
        {
            return JsonConvert.DeserializeObject(WriteErrorJsonString);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return JsonConvert.SerializeObject(ToObject());
        }
    }
}
