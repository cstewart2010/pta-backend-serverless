using TheReplacement.PTA.Api.Services.Enums;

namespace TheReplacement.PTA.Api.Services.Models
{
    /// <summary>
    /// Represents a participant to an encounter during a PTA session
    /// </summary>
    public class SettingParticipantModel
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SettingParticipantModel() { }

        /// <summary>
        /// Initiziles a new instance of <see cref="SettingParticipantModel"/>
        /// </summary>
        /// <param name="currentHp">The participant's current hp</param>
        /// <param name="totalHp">The participant's total hp</param>
        public SettingParticipantModel(double currentHp, double totalHp)
        {
            Health = GetHealth(currentHp, totalHp);
        }

        /// <summary>
        /// The paticipant's id
        /// </summary>
        public Guid ParticipantId { get; set; }

        /// <summary>
        /// The paticipant's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The pariticipant's type (Trainer/Pokemon/Npc)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// A description of the participant's health
        /// </summary>
        public string Health { get; set; }

        /// <summary>
        /// The pariticipant's speed
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// The participants's position on the map
        /// </summary>
        public MapPositionModel Position { get; set; }

        /// <summary>
        /// Returns the trainer as a participant
        /// </summary>
        /// <param name="trainerId"></param>
        /// <param name="gameId"></param>
        /// <param name="position"></param>
        public static SettingParticipantModel FromTrainer(Guid trainerId, Guid gameId, MapPositionModel position)
        {
            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            return new SettingParticipantModel(trainer.CurrentHP, trainer.TrainerStats.HP)
            {
                ParticipantId = trainer.TrainerId,
                Name = trainer.TrainerName,
                Type = "Trainer",
                Speed = trainer.TrainerStats.Speed,
                Position = position
            };
        }

        /// <summary>
        /// Returns the Shop as a participant
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="gameId"></param>
        /// <param name="position"></param>
        public static SettingParticipantModel FromShop(Guid shopId, Guid gameId, MapPositionModel position)
        {
            var shop = DatabaseUtility.FindShopById(shopId, gameId);
            return new SettingParticipantModel
            {
                ParticipantId = shop.ShopId,
                Name = shop.Name,
                Type = SettingParticipantType.Shop.ToString(),
                Position = position
            };
        }

        /// <summary>
        /// Returns the pokemon as a participant
        /// </summary>
        /// <param name="pokemonId"></param>
        /// <param name="position"></param>
        /// <param name="type"></param>
        public static SettingParticipantModel FromPokemon(Guid pokemonId, MapPositionModel position, SettingParticipantType type)
        {
            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            return new SettingParticipantModel(pokemon.CurrentHP, pokemon.PokemonStats.HP)
            {
                ParticipantId = pokemon.PokemonId,
                Name = pokemon.Nickname,
                Type = type.ToString(),
                Speed = pokemon.PokemonStats.Speed,
                Position = position
            };
        }

        /// <summary>
        /// Returns the npc as a participant
        /// </summary>
        /// <param name="npcId"></param>
        /// <param name="position"></param>
        /// <param name="type"></param>
        public static SettingParticipantModel FromNpc(Guid npcId, MapPositionModel position, SettingParticipantType type)
        {
            var npc = DatabaseUtility.FindNpc(npcId);
            return new SettingParticipantModel(npc.CurrentHP, npc.TrainerStats.HP)
            {
                ParticipantId = npc.NPCId,
                Name = npc.TrainerName,
                Type = type.ToString(),
                Speed = npc.TrainerStats.Speed,
                Position = position
            };
        }

        private static string GetHealth(double currentHp, double totalHp)
        {
            var quotient = currentHp / totalHp;
            if (quotient >= 1)
            {
                return "Feeling fresh!";
            }
            else if (quotient > .6)
            {
                return "Going strong!";
            }
            else if (quotient > .3)
            {
                return "Might need some help. . .";
            }
            else if (quotient > 0)
            {
                return "Help!!!";
            }
            else if (quotient > -1)
            {
                return "Incapacitated";
            }

            return "";
        }
    }
}
