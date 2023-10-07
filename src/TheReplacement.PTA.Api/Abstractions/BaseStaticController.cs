using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Abstractions
{
    public abstract class BaseStaticController
    {
        public StaticCollectionMessage GetStaticCollectionResponse<TDocument>(
            IEnumerable<TDocument> documents,
            HttpRequest request) where TDocument : IDexDocument
        {
            if (!int.TryParse(request.Query["offset"], out var offset))
            {
                offset = 0;
            }
            if (!int.TryParse(request.Query["limit"], out var limit))
            {
                limit = 20;
            }

            var hostUrl = $"{request.Scheme}://{request.Host}{request.Path}".Trim('/');
            var count = documents.Count();
            var previous = GetPreviousUrl(offset, limit, hostUrl);
            var next = GetNextUrl(offset, limit, count, hostUrl);
            var results = documents.GetSubset(offset, limit)
                .Select(doc => GetResultsMember(doc, hostUrl));

            return new StaticCollectionMessage
            (
                count,
                previous,
                next,
                results
            );
        }

        protected string GetPreviousUrl(int offset, int limit, string hostUrl)
        {
            int previousOffset = Math.Max(0, offset - limit);
            int previousLimit = offset - limit < 0 ? offset : limit;
            if (previousOffset == offset)
            {
                return null;
            }

            return $"{hostUrl}?offset={previousOffset}&limit={previousLimit}";
        }

        protected string GetNextUrl(int offset, int limit, int count, string hostUrl)
        {
            int nextOffset = Math.Min(offset + limit, count - limit);
            if (nextOffset <= offset)
            {
                return null;
            }

            return $"{hostUrl}?offset={nextOffset}&limit={limit}";
        }

        protected ResultMessage GetResultsMember<TDocument>(TDocument document, string hostUrl) where TDocument : IDexDocument
        {
            if (document == null)
            {
                return null;
            }

            return new ResultMessage
            {
                Name = document.Name,
                Url = $"{hostUrl}/{document.Name.Replace("/", "_")}"
            };
        }
    }
}
