using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services.Internal
{
    internal class ExportedGame
    {
        public ExportedGame() { }

        public ExportedGame(GameModel game)
        {
            GameSession = game;
            var trainers = DatabaseUtility.FindTrainersByGameId(game.GameId);
            Trainers = trainers.Select(trainer => new ExportedTrainer(trainer));
        }

        public GameModel GameSession { get; set; }
        public IEnumerable<ExportedTrainer> Trainers { get; set; }
    }
}
