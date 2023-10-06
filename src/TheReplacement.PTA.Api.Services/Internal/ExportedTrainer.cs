using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services.Internal
{
    internal class ExportedTrainer
    {
        public ExportedTrainer() { }

        public ExportedTrainer(TrainerModel trainer)
        {
            trainer.IsOnline = false;
            Trainer = trainer;
            DatabaseUtility.UpdateTrainerOnlineStatus(trainer.TrainerId, false);
            Pokemon = DatabaseUtility.FindPokemonByTrainerId(trainer.TrainerId, trainer.GameId);
        }
        public TrainerModel Trainer { get; set; }
        public IEnumerable<PokemonModel> Pokemon { get; set; }
    }
}
