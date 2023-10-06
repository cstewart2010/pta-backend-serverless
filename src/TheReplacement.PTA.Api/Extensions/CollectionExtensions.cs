using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Services.Interfaces;

namespace TheReplacement.PTA.Api.Extensions
{
    public static class CollectionExtensions
    {
        public static TDocument GetStaticDocument<TDocument>(
            this IEnumerable<TDocument> source,
            string name) where TDocument : IDexDocument
        {
            if (string.IsNullOrEmpty(name))
            {
                return default;
            }

            if (int.TryParse(name, out var index))
            {
                return GetStaticDocument(source, index);
            }

            return source.FirstOrDefault(document => string.Equals(document.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }

        public static IEnumerable<TDocument> GetSubset<TDocument>(
            this IEnumerable<TDocument> source,
            int offset,
            int limit)
        {
            if (offset < 0 || offset >= source.Count() || limit < 0)
            {
                return Array.Empty<TDocument>();
            }

            if (limit >= source.Count())
            {
                return source.Skip(offset);
            }

            if (offset + limit > source.Count())
            {
                limit = source.Count() - offset;
            }

            return source.Skip(offset).Take(limit);
        }

        private static TDocument GetStaticDocument<TDocument>(
            IEnumerable<TDocument> source,
            int index) where TDocument : IDexDocument
        {
            if (index > 0 && index <= source.Count())
            {
                return source.ElementAt(index - 1);
            }

            return default;
        }

    }
}
