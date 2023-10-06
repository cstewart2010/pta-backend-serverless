using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Messages
{
    public class AllLogsMessage : AbstractMessage
    {
        internal AllLogsMessage(GameModel game)
        {
            Message = "Game logs";
            LogPages = new List<IEnumerable<LogModel>>();
            if (game.Logs == null)
            {
                return;
            }

            var logs = game.Logs.OrderByDescending(log => log.LogTimestamp);
            for (int i = 0; i < logs.Count(); i += 50)
            {
                LogPages.Add(GetPage(logs, i, 50));
            }
        }

        private static IEnumerable<LogModel> GetPage(
            IEnumerable<LogModel> source,
            int offset,
            int limit)
        {
            if (offset < 0 || offset >= source.Count() || limit < 0)
            {
                return Array.Empty<LogModel>();
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

        public override string Message { get; }

        public List<IEnumerable<LogModel>> LogPages { get;  }
    }
}
