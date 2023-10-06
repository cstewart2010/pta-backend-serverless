using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Controllers
{
    public abstract class BaseStaticController
    {
        public StaticCollectionMessage GetStaticCollectionResponse<TDocument>(
            IEnumerable<TDocument> documents,
            int offset,
            int limit) where TDocument : IDexDocument
        {
            var count = documents.Count();
            var previous = GetPreviousUrl(offset, limit);
            var next = GetNextUrl(offset, limit, count);
            var results = documents.GetSubset(offset, limit)
                .Select(GetResultsMember);

            return new StaticCollectionMessage
            (
                count,
                previous,
                next,
                results
            );
        }

        protected string GetPreviousUrl(int offset, int limit)
        {
            int previousOffset = Math.Max(0, offset - limit);
            int previousLimit = offset - limit < 0 ? offset : limit;
            if (previousOffset == offset)
            {
                return null;
            }

            return $"?offset={previousOffset}&limit={previousLimit}";
        }

        protected string GetNextUrl(int offset, int limit, int count)
        {
            int nextOffset = Math.Min(offset + limit, count - limit);
            if (nextOffset <= offset)
            {
                return null;
            }

            return $"?offset={nextOffset}&limit={limit}";
        }

        protected ResultMessage GetResultsMember<TDocument>(TDocument document) where TDocument : IDexDocument
        {
            if (document == null)
            {
                return null;
            }

            return new ResultMessage
            {
                Name = document.Name,
                Url = $"{document.Name.Replace("/", "_")}"
            };
        }
    }
}
