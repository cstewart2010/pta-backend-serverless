namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a container for valid MongoCollection to perform CRUD operations on
    /// </summary>
    public enum MongoCollection
    {
        /// <summary>
        /// Represents the Games collection
        /// </summary>
        Games = 1,

        /// <summary>
        /// Represents the Npcs collection
        /// </summary>
        Npcs = 2,

        /// <summary>
        /// Represents the Pokemon collection
        /// </summary>
        Pokemon = 3,

        /// <summary>
        /// Represents the Trainers collection
        /// </summary>
        Trainers = 4,

        /// <summary>
        /// Represents the PokeDex collection
        /// </summary>
        PokeDex = 5,

        /// <summary>
        /// Represents the Settings collection
        /// </summary>
        Settings = 6,

        /// <summary>
        /// Represents the Users collection
        /// </summary>
        Users = 7,

        /// <summary>
        /// Represents the UserMessageThreads collection
        /// </summary>
        UserMessageThreads = 8
    }
}
