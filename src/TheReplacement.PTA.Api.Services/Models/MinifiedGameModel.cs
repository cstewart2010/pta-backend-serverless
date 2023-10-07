using MongoDB.Driver;
using TheReplacement.PTA.Api.Services.Internal;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a Game Session for external use
    /// </summary>
    public class MinifiedGameModel
    {
        public MinifiedGameModel(GameModel game)
        {
            GameId = game.GameId;
            Nickname = game.Nickname;
            GameMasters = MongoCollectionHelper.Trainers
                .Find(trainer => trainer.GameId == game.GameId && trainer.IsGM)
                .ToEnumerable()
                .Select(trainer => trainer.TrainerName);
        }

        /// <summary>
        /// The PTA game session id
        /// </summary>
        public Guid GameId { get; }

        /// <summary>
        /// The PTA game masters
        /// </summary>
        public IEnumerable<string> GameMasters{ get; }

        /// <summary>
        /// A user-friendly nickname for the game session
        /// </summary>
        public string Nickname { get; }
    }
}
