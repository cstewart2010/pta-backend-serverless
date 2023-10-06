namespace TheReplacement.PTA.Api.Services.Interfaces
{
    /// <summary>
    /// Provides a collection of properties for Authenticated types
    /// </summary>
    public interface IAuthenticated
    {
        /// <summary>
        /// An encrypted password
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// The online status
        /// </summary>
        public bool IsOnline { get; set; }
    }
}
