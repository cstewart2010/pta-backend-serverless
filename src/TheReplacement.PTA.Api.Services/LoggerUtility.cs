using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Internal;

namespace TheReplacement.PTA.Api.Services
{
    /// <summary>
    /// Provides a collection of methods for record events to the database
    /// </summary>
    public static class LoggerUtility
    {
        /// <summary>
        /// Writes an INFO-level message to the database
        /// </summary>
        /// <param name="affectedCollection">The collection where the action occured</param>
        /// <param name="record">A record of the action</param>
        private static void Info(
            MongoCollection affectedCollection,
            object record)
        {
            LogMessage
            (
                LogLevel.Info,
                affectedCollection,
                record
            );
        }

        /// <summary>
        /// Writes an ERROR-level message to the database
        /// </summary>
        /// <param name="affectedCollection">The collection where the error was thrown</param>
        /// <param name="record">A record of the error</param>
        private static void Error(
            MongoCollection affectedCollection,
            object record)
        {
            LogMessage
            (
                LogLevel.Error,
                affectedCollection,
                record
            );
        }

        private static void LogMessage(
            LogLevel level,
            MongoCollection affectedCollection,
            object record)
        {
            MongoCollectionHelper.Logs.InsertOne(new Models.LoggerModel
            {
                AffectedCollection = affectedCollection.ToString(),
                LogLevel = level.ToString(),
                Timestamp = DateTime.Now,
                Message = record.ToString()
            });
        }
    }
}
