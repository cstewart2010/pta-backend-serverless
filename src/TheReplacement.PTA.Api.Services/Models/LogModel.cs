namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a Pokemon Tabletop Adventures log
    /// </summary>
    public class LogModel
    {
        private LogModel() { }

        public LogModel(string user, string action)
        {
            User = user;
            Action = action;
            LogTimestamp = DateTime.UtcNow;
        }
        /// <summary>
        /// The user that the log comes from
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// The action being logged
        /// </summary>
        public string Action { get; set; }

        public DateTime? LogTimestamp { get; set; }
    }
}
