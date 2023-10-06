using System.Collections.Generic;

namespace TheReplacement.PTA.Api.Messages
{
    public class StaticCollectionMessage
    {
        internal StaticCollectionMessage(
            int count,
            string previousUrl,
            string nextUrl,
            IEnumerable<ResultMessage> results)
        {
            Count = count;
            Previous = previousUrl;
            Next = nextUrl;
            Results = results;
        }

        public int Count { get; }
        public string Previous { get; }
        public string Next { get; }
        public IEnumerable<ResultMessage> Results { get; }
    }
}
