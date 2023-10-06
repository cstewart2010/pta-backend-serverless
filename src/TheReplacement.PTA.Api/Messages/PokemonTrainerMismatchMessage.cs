using System;

namespace TheReplacement.PTA.Api.Messages
{
    public class PokemonTrainerMismatchMessage : AbstractMessage
    {
        internal PokemonTrainerMismatchMessage(
            Guid pokemonTrainerId,
            Guid trainerId)
        {
            Message = "Invalid trainerId";
            ExpectedTrainerId = trainerId;
            PokemonTrainerId = pokemonTrainerId;
        }

        public override string Message { get; }
        public Guid ExpectedTrainerId { get; }
        public Guid PokemonTrainerId { get; }
    }
}
