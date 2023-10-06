namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a User's Role within the PTA App
    /// </summary>
    public enum UserRoleOnSite
    {
        /// <summary>
        /// Represents a user that has been IP banned
        /// </summary>
        IpBanned = -1,

        /// <summary>
        /// Represents a user that is voluntarily no longer active
        /// </summary>
        Deactivated = 0,

        /// <summary>
        /// Represents a user that is still active within the site
        /// </summary>
        Active = 1,

        /// <summary>
        /// Represents a user with site-wide admin privileges
        /// </summary>
        SiteAdmin = 2
    }
}
