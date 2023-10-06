using System.Collections.Generic;

namespace TheReplacement.PTA.Api.Messages
{
    public class InvalidQueryStringMessage : AbstractMessage
    {
        public InvalidQueryStringMessage()
        {
            Message = "Missing the following parameters in the query";
        }

        public override string Message { get; }
        public IEnumerable<string> MissingParameters { get; set; }
        public IEnumerable<string> InvalidParameters { get; set; }
    }
}
