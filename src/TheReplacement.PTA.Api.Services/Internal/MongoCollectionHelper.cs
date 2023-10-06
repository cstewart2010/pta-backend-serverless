using MongoDB.Driver;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services.Internal
{
    internal static class MongoCollectionHelper
    {
        static MongoCollectionHelper()
        {
            var settings = GetMongoClientSettings();
            var client = new MongoClient(settings);
            var databaseName = Environment.GetEnvironmentVariable("Database", EnvironmentVariableTarget.Process);
            Database = client.GetDatabase(databaseName);
            Games = Database.GetCollection<GameModel>(MongoCollection.Games.ToString());
            Pokemon = Database.GetCollection<PokemonModel>(MongoCollection.Pokemon.ToString());
            Trainers = Database.GetCollection<TrainerModel>(MongoCollection.Trainers.ToString());
            Users = Database.GetCollection<UserModel>(MongoCollection.Users.ToString());
            UserMessageThreads = Database.GetCollection<UserMessageThreadModel>(MongoCollection.UserMessageThreads.ToString());
            Npcs = Database.GetCollection<NpcModel>("NPCs");
            Logs = Database.GetCollection<LoggerModel>("Logs");
            PokeDex = Database.GetCollection<PokeDexItemModel>(MongoCollection.PokeDex.ToString());
            Settings = Database.GetCollection<SettingModel>(MongoCollection.Settings.ToString());
            Shops = Database.GetCollection<ShopModel>("Shops");
            Sprite = Database.GetCollection<SpriteModel>("Sprites");
        }

        /// <summary>
        /// Represents the BasePokemon Collection
        /// </summary>
        public static IMongoDatabase Database { get; }

        /// <summary>
        /// Represents the Game Collection
        /// </summary>
        public static IMongoCollection<GameModel> Games { get; }

        /// <summary>
        /// Represents the Pokemon Collection
        /// </summary>
        public static IMongoCollection<PokemonModel> Pokemon { get; }

        /// <summary>
        /// Represents the Trainer Collection
        /// </summary>
        public static IMongoCollection<TrainerModel> Trainers { get; }

        /// <summary>
        /// Represents the User Collection
        /// </summary>
        public static IMongoCollection<UserModel> Users { get; }

        /// <summary>
        /// Represents the User Collection
        /// </summary>
        public static IMongoCollection<UserMessageThreadModel> UserMessageThreads { get; }

        /// <summary>
        /// Represents the Npc Collection
        /// </summary>
        public static IMongoCollection<NpcModel> Npcs { get; }

        /// <summary>
        /// Represents the PokeDex Collection
        /// </summary>
        public static IMongoCollection<PokeDexItemModel> PokeDex { get; }

        /// <summary>
        /// Represents the Settings Collection
        /// </summary>
        public static IMongoCollection<SettingModel> Settings { get; }

        /// <summary>
        /// Represents the Shops Collection
        /// </summary>
        public static IMongoCollection<ShopModel> Shops { get; }

        /// <summary>
        /// Represents the Sprites Collection
        /// </summary>
        public static IMongoCollection<SpriteModel> Sprite { get; }

        /// <summary>
        /// Represents the Logs Collection
        /// </summary>
        public static IMongoCollection<LoggerModel> Logs { get; }

        private static MongoClientSettings GetMongoClientSettings()
        {
            var connectionString = Environment.GetEnvironmentVariable("MongoDBConnectionString", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new NullReferenceException("MongoDBConnectionString environment variable need to be set to access MongoDB");
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            return settings;
        }
    }
}
