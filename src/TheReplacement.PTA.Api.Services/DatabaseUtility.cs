using MongoDB.Driver;
using System.Linq.Expressions;
using TheReplacement.PTA.Api.Services.Internal;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services
{
    /// <summary>
    /// Provides a Collection of CRUD methods for the PTA database
    /// </summary>
    public static class DatabaseUtility
    {
        /// <summary>
        /// Searches for a encounter using its id, then deletes it
        /// </summary>
        /// <param name="id">The encounter id</param>
        public static bool DeleteSetting(Guid id)
        {
            return MongoCollectionHelper
                .Settings
                .FindOneAndDelete(encounter => encounter.SettingId == id) != null;
        }

        /// <summary>
        /// Searches for encounter using their game id, then deletes them
        /// </summary>
        /// <param name="gameId">The game session id</param>
        public static bool DeleteSettingsByGameId(Guid gameId)
        {
            return MongoCollectionHelper
                .Settings
                .DeleteMany(encounter => encounter.GameId == gameId)?.IsAcknowledged == true;
        }

        /// <summary>
        /// Searches for a game using its id, then deletes it
        /// </summary>
        /// <param name="id">The game session id</param>
        public static bool DeleteGame(Guid id)
        {
            var gameDeletionResult = MongoCollectionHelper
                .Games
                .FindOneAndDelete(game => game.GameId == id) != null;

            var trainers = MongoCollectionHelper
                .Trainers
                .Find(trainer => trainer.GameId == id)
                .ToEnumerable()
                .Select(trainer => trainer.TrainerId);

            var users = MongoCollectionHelper
                .Users
                .Find(user => trainers.Contains(user.UserId))
                .ToEnumerable();

            foreach (var user in users)
            {
                user.Games.Remove(id);
                UpdateUser(user);
            }

            var trainerDeletionResult = MongoCollectionHelper
                .Trainers
                .DeleteMany(trainer => trainers.Contains(trainer.TrainerId)).IsAcknowledged;

            var encounterDeletionResult = MongoCollectionHelper
                .Settings
                .DeleteMany(encounter => encounter.GameId == id);

            var pokedexDeletionResult = MongoCollectionHelper
                .PokeDex
                .DeleteMany(pokedex => trainers.Contains(pokedex.TrainerId)).IsAcknowledged;

            var pokemonDeletionResult = MongoCollectionHelper
                .Pokemon
                .DeleteMany(pokemon => pokemon.GameId == id).IsAcknowledged;

            var npcDeletionResult = DeleteNpcByGameId(id);
            var shopDeletionResult = DeleteShopByGameId(id);
            return gameDeletionResult && trainerDeletionResult && pokedexDeletionResult && pokemonDeletionResult && npcDeletionResult && shopDeletionResult;
        }

        /// <summary>
        /// Searches for an npc using its id, then deletes it
        /// </summary>
        /// <param name="id">The npc id</param>
        public static bool DeleteNpc(Guid id)
        {
            return MongoCollectionHelper
                .Npcs
                .FindOneAndDelete(npc => npc.NPCId == id) != null;
        }

        /// <summary>
        /// Searches for all Npcs using their game id, then deletes it
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static bool DeleteNpcByGameId(Guid gameId)
        {
            var deleteResult = MongoCollectionHelper
                .Npcs
                .DeleteMany(npc => npc.GameId == gameId);

            return deleteResult.IsAcknowledged;
        }

        /// <summary>
        /// Searches for a Pokemon using its id, then deletes it
        /// </summary>
        /// <param name="id">The Pokemon id</param>
        public static bool DeletePokemon(Guid id)
        {
            return MongoCollectionHelper
                .Pokemon
                .FindOneAndDelete(pokemon => pokemon.PokemonId == id) != null;
        }

        /// <summary>
        /// Searches for all Pokemon using their trainer id, then deletes it
        /// </summary>
        /// <param name="gameId">The game id</param>
        /// <param name="trainerId">The trainer id</param>
        public static long DeletePokemonByTrainerId(Guid gameId, Guid trainerId)
        {
            var deleteResult = MongoCollectionHelper
                .Pokemon
                .DeleteMany(pokemon => pokemon.TrainerId == trainerId && pokemon.GameId == gameId);
            
            if (deleteResult.IsAcknowledged)
            {
                MongoCollectionHelper.PokeDex.DeleteMany(pokeDex => pokeDex.TrainerId == trainerId && pokeDex.GameId == gameId);
                return deleteResult.DeletedCount;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Searches for a shop using their id, then deletes it
        /// </summary>
        /// <param name="id">The shop id</param>
        /// <param name="gameId">The game id</param>
        public static bool DeleteShop(Guid id, Guid gameId)
        {
            return MongoCollectionHelper
                .Shops
                .FindOneAndDelete(shop => shop.ShopId == id && shop.GameId == gameId) != null;
        }


        /// <summary>
        /// Searches for all shops using the game id, then deletes it
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static bool DeleteShopByGameId(Guid gameId)
        {
            var deleteResult = MongoCollectionHelper
                .Shops
                .DeleteMany(shop => shop.GameId == gameId);

            return deleteResult.IsAcknowledged;
        }

        /// <summary>
        /// Searches for a trainer using their id, then deletes it
        /// </summary>
        /// <param name="gameId">The game id</param>
        /// <param name="userId">The user's id</param>
        public static bool DeleteTrainer(Guid gameId, Guid userId)
        {
            var trainer = MongoCollectionHelper
                .Trainers
                .FindOneAndDelete(trainer => trainer.TrainerId == userId && trainer.GameId == gameId);

            if (trainer.IsGM)
            {
                return DeleteGame(trainer.GameId);
            }
            else
            {
                var pokemonDeletionResult = MongoCollectionHelper
                    .Pokemon
                    .DeleteMany(pokemon => pokemon.TrainerId == userId && pokemon.GameId == gameId).IsAcknowledged;

                var pokedexDeletionResult = MongoCollectionHelper
                    .PokeDex
                    .DeleteMany(pokedex => pokedex.TrainerId == userId && pokedex.GameId == gameId).IsAcknowledged;

                var settings = FindAllSettings(gameId);
                foreach (var encounter in settings)
                {
                    encounter.ActiveParticipants = encounter.ActiveParticipants.Where(participant => participant.ParticipantId != userId);
                    UpdateSetting(encounter);
                }

                var user = FindUserById(userId);
                user.Games.Remove(gameId);
                UpdateUser(user);

                return pokedexDeletionResult && pokemonDeletionResult;
            }
        }

        /// <summary>
        /// Deletes the user and everything associated with them
        /// </summary>
        /// <param name="userId">The user's user id</param>
        public static bool DeleteUser(Guid userId)
        {
            var userDeletionResult = MongoCollectionHelper
                .Users
                .FindOneAndDelete(user => user.UserId == userId) != null;

            var games = MongoCollectionHelper
                .Trainers
                .Find(trainer => trainer.TrainerId == userId)
                .ToEnumerable()
                .Select(trainer => trainer.GameId);

            var trainerDeletionResult = true;
            foreach (var game in games)
            {
                trainerDeletionResult = trainerDeletionResult && DeleteTrainer(game, userId);
            }

            return userDeletionResult && trainerDeletionResult;
        }



        /// <summary>
        /// Searches for all trainers using their game id, then deletes it
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static long DeleteTrainersByGameId(Guid gameId)
        {
            foreach (var trainer in FindTrainersByGameId(gameId))
            {
                var user = FindUserById(trainer.TrainerId);
                user.Games.Remove(gameId);
                UpdateUser(user);
            }
            var deleteResult =  MongoCollectionHelper
                .Trainers
                .DeleteMany(trainer => trainer.GameId == gameId);

            if (deleteResult.IsAcknowledged)
            {
                return deleteResult.DeletedCount;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns an active encounter (if any) matching the game session id
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static SettingModel FindActiveSetting(Guid gameId)
        {
            return MongoCollectionHelper.Settings
                .Find(encounter => encounter.GameId == gameId && encounter.IsActive)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns an setting matching the id
        /// </summary>
        /// <param name="encounterId">The setting id</param>
        public static SettingModel FindSetting(Guid encounterId)
        {
            return MongoCollectionHelper.Settings
                .Find(encounter => encounter.SettingId == encounterId)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns all settings associated with the game session
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static IEnumerable<SettingModel> FindAllSettings(Guid gameId)
        {
            return MongoCollectionHelper.Settings
                .Find(encounter => encounter.GameId == gameId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns all games in db
        /// </summary>
        public static IEnumerable<MinifiedGameModel> FindMostRecent20Games(UserModel user)
        {
            var games = MongoCollectionHelper.Games
                .Find(game => !user.Games.Contains(game.GameId))
                .ToEnumerable()
                .Select(game => new MinifiedGameModel(game));

            if (games.Count() > 20)
            {
                return games.TakeLast(20);
            }

            return games;
        }

        /// <summary>
        /// Returns all games that contains the supplied nickname as a substring
        /// </summary>
        /// <param name="nickname">The nickname to search with</param>
        public static IEnumerable<GameModel> FindAllGames(string nickname)
        {
            return MongoCollectionHelper.Games
                .Find(game => game.Nickname.ToLower().Contains(nickname.ToLower()))
                .ToEnumerable();
        }

        /// <summary>
        /// Returns all games that the user is a part of
        /// </summary>
        /// <param name="user">The user to search with</param>
        public static IEnumerable<GameModel> FindAllGamesWithUser(UserModel user)
        {
            return MongoCollectionHelper.Games
                .Find(game => user.Games.Contains(game.GameId))
                .ToEnumerable();
        }

        /// <summary>
        /// Returns a game matching the game session id
        /// </summary>
        /// <param name="id">The game session id</param>
        public static GameModel FindGame(Guid id)
        {
            return MongoCollectionHelper
                .Games
                .Find(game => game.GameId == id)
                .SingleOrDefault();
        }


        /// <summary>
        /// Returns an npc matching the npc id
        /// </summary>
        /// <param name="id">The npc id</param>
        public static NpcModel FindNpc(Guid id)
        {
            return MongoCollectionHelper
                .Npcs
                .Find(npc => npc.NPCId == id)
                .SingleOrDefault();
        }

        /// <summary>
        /// Attempts to update the trainer with their appropriate starting stats
        /// </summary>
        /// <param name="trainerId">The id of the trainer being updated</param>
        /// <param name="origin">The trianer's origin</param>
        /// <param name="trainerClass">The trainer's stats class</param>
        /// <param name="feats">The trainer's starting feats</param>
        /// <param name="stats">The trainer's starting stats</param>
        /// <returns>True if successful</returns>
        public static bool CompleteTrainer(
            Guid trainerId,
            string origin,
            string trainerClass,
            IEnumerable<string> feats,
            StatsModel stats)
        {
            var updates = Builders<TrainerModel>.Update.Combine(new[]
            {
                Builders<TrainerModel>.Update.Set("Origin", origin),
                Builders<TrainerModel>.Update.Set("TrainerClasses", new[] { trainerClass }),
                Builders<TrainerModel>.Update.Set("Feats", feats),
                Builders<TrainerModel>.Update.Set("TrainerStats", stats),
                Builders<TrainerModel>.Update.Set("IsComplete", true)
            });

            return TryUpdateDocument
            (
                MongoCollectionHelper.Trainers,
                trainer => trainer.TrainerId == trainerId,
                updates
            );
        }

        /// <summary>
        /// Returns a message thread matching the id
        /// </summary>
        /// <param name="id">The message id</param>
        public static UserMessageThreadModel FindMessageById(Guid id)
        {
            return MongoCollectionHelper
                .UserMessageThreads
                .Find(message => message.MessageId == id)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns all npcs matching the npc ids
        /// </summary>
        /// <param name="npcIds">The npc ids</param>
        public static IEnumerable<NpcModel> FindNpcs(IEnumerable<Guid> npcIds)
        {
            var npcs = npcIds == null
                ? throw new ArgumentNullException(nameof(npcIds))
                : MongoCollectionHelper
                .Npcs
                .Find(npc => npcIds.Contains(npc.NPCId));

            return npcs.ToEnumerable();
        }

        /// <summary>
        /// Returns all npcs matching the game id
        /// </summary>
        /// <param name="gameId">The npc ids</param>
        public static IEnumerable<NpcModel> FindNpcsByGameId(Guid gameId)
        {
            return MongoCollectionHelper.Npcs.Find(npc => npc.GameId == gameId).ToEnumerable();
        }

        /// <summary>
        /// Returns a Pokemon matching the Pokemon id
        /// </summary>
        /// <param name="id">The Pokemon id</param>
        public static PokemonModel FindPokemonById(Guid id)
        {
            return MongoCollectionHelper
                .Pokemon
                .Find(Pokemon => Pokemon.PokemonId == id)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns all Pokemon matching the trainer id
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        public static IEnumerable<PokemonModel> FindPokemonByTrainerId(Guid trainerId)
        {
            return MongoCollectionHelper
                .Pokemon
                .Find(pokemon => pokemon.TrainerId == trainerId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns all Pokemon matching the trainer id for a certain game session
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        /// <param name="gameId">The game session id</param>
        public static IEnumerable<PokemonModel> FindPokemonByTrainerId(Guid trainerId, Guid gameId)
        {
            return MongoCollectionHelper
                .Pokemon
                .Find(pokemon => pokemon.TrainerId == trainerId && pokemon.GameId == gameId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns a shop matching the id
        /// </summary>
        /// <param name="id">The shop id</param>
        /// <param name="gameId">The game id</param>
        public static ShopModel FindShopById(Guid id, Guid gameId)
        {
            return MongoCollectionHelper
                .Shops
                .Find(shop => shop.ShopId == id && shop.GameId == gameId)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns a shops matches contained in the setting
        /// </summary>
        /// <param name="setting">The setting in the game</param>
        public static IEnumerable<ShopModel> FindShopsBySetting(SettingModel setting)
        {
            return MongoCollectionHelper
                .Shops
                .Find(shop => setting.Shops.Contains(shop.ShopId) && setting.GameId == shop.GameId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns a shop contained in the game
        /// </summary>
        /// <param name="gameId">The game id</param>
        public static IEnumerable<ShopModel> FindShopsByGameId(Guid gameId)
        {
            return MongoCollectionHelper
                .Shops
                .Find(shop => shop.GameId == gameId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns a trainer matching the trainer id
        /// </summary>
        /// <param name="id">The trainer id</param>
        /// <param name="gameId">The game session id</param>
        public static TrainerModel FindTrainerById(Guid id, Guid gameId)
        {
            return FindTrainerById(trainer => trainer.TrainerId == id && trainer.GameId == gameId);
        }

        /// <summary>
        /// Returns a user matching the trainer id
        /// </summary>
        /// <param name="id">The user id</param>
        public static UserModel FindUserById(Guid id)
        {
            var user = MongoCollectionHelper
                .Users
                .Find(user => user.UserId == id)
                .SingleOrDefault();

            return user;
        }

        /// <summary>
        /// Returns all users in the database
        /// </summary>
        public static IEnumerable<UserModel> FindUsers()
        {
            return MongoCollectionHelper
                .Users
                .Find(user => true)
                .ToEnumerable();
        }

        /// <summary>
        /// Search for the trainer and returns them if the trainer has not completed the new user flow
        /// </summary>
        /// <param name="id">The id of the trainer to search for</param>
        public static TrainerModel FindIncompleteTrainerById(Guid id)
        {
            return FindTrainerById(trainer => trainer.TrainerId == id && !trainer.IsComplete);
        }

        /// <summary>
        /// Returns all trainers matching the game session id
        /// </summary>
        /// <param name="gameId">The game session id</param>
        public static IEnumerable<TrainerModel> FindTrainersByGameId(Guid gameId)
        {
            return MongoCollectionHelper
                .Trainers
                .Find(trainer => trainer.GameId == gameId)
                .ToEnumerable();
        }

        /// <summary>
        /// Returns a trainer matching the trainer name and game session id
        /// </summary>
        /// <param name="username">The trainer name</param>
        /// <param name="gameId">The game session id</param>
        public static TrainerModel FindTrainerByUsername(
            string username,
            Guid gameId)
        {
            Expression<Func<TrainerModel, bool>> filter = trainer => trainer.TrainerName.ToLower() == username.ToLower() && trainer.GameId == gameId;

            return MongoCollectionHelper
                .Trainers
                .Find(filter)
                .SingleOrDefault(); ;
        }

        /// <summary>
        /// Returns a trainer matching the trainer name and game session id
        /// </summary>
        /// <param name="username">The trainer name</param>
        public static UserModel FindUserByUsername(string username)
        {
            Expression<Func<UserModel, bool>> filter = user => user.Username.ToLower() == username.ToLower();

            return MongoCollectionHelper
                .Users
                .Find(filter)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns all sprites
        /// </summary>
        public static IEnumerable<SpriteModel> GetAllSprites()
        {
            return MongoCollectionHelper.Sprite.Find(sprite => true).ToEnumerable();
        }

        /// <summary>
        /// Returns a game's nickname using the game id
        /// </summary>
        /// <param name="gameId">The game session id</param>
        public static string GetGameNickname(Guid gameId)
        {
            return FindGame(gameId)?.Nickname;
        }

        /// <summary>
        /// Compiles all pokedex entries for a specific trainer into one collection
        /// </summary>
        /// <param name="trainerId">The trainer's id to search with</param>
        public static IEnumerable<PokeDexItemModel> GetTrainerPokeDex(Guid trainerId)
        {
            return MongoCollectionHelper.PokeDex
                .Find(dexItem => dexItem.TrainerId == trainerId)
                .ToEnumerable();
        }

        /// <summary>
        /// Searches the database for a pokedex entry
        /// </summary>
        /// <param name="trainerId">The trainer's id to search with</param>
        /// <param name="gameId">The game session id</param>
        /// <param name="dexNo">The dex number for the pokemon</param>
        public static PokeDexItemModel GetPokedexItem(Guid trainerId, Guid gameId, int dexNo)
        {
            return MongoCollectionHelper.PokeDex
                .Find(dexItem => dexItem.TrainerId == trainerId && dexItem.GameId == gameId && dexItem.DexNo == dexNo)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns whether there is a game master for the provide game session
        /// </summary>
        /// <param name="gameId">The game session id</param>
        /// <param name="error">The error</param>
        public static bool HasGM(
            Guid gameId,
            out object error)
        {
            error = new
            {
                message = "No GM has been made",
                gameId
            };

            return FindGame(gameId) != null && MongoCollectionHelper
                .Trainers
                .Find(trainer => trainer.IsGM && trainer.GameId == gameId)
                .Any();
        }

        /// <summary>
        /// Attempts to replace the previous encounter with the new data
        /// </summary>
        /// <param name="updatedSetting">The updated encounter data</param>
        public static bool UpdateSetting(SettingModel updatedSetting)
        {
            var result = MongoCollectionHelper.Settings.ReplaceOne
            (
                encounter => encounter.SettingId == updatedSetting.SettingId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedSetting
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous Npc with the new data
        /// </summary>
        /// <param name="updatedNpc">The updated npc data</param>
        public static bool UpdateNpc(NpcModel updatedNpc)
        {
            var result = MongoCollectionHelper.Npcs.ReplaceOne
            (
                npc => npc.NPCId == updatedNpc.NPCId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedNpc
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous pokemon with the new data
        /// </summary>
        /// <param name="updatePokemon">The updated pokemon data</param>
        public static bool UpdatePokemon(PokemonModel updatePokemon)
        {
            var result = MongoCollectionHelper.Pokemon.ReplaceOne
            (
                pokemon => pokemon.PokemonId == updatePokemon.PokemonId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatePokemon
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous shop with the new data
        /// </summary>
        /// <param name="updatedShop">the update shop data</param>
        public static bool UpdateShop(ShopModel updatedShop)
        {
            var result = MongoCollectionHelper.Shops.ReplaceOne
            (
                shop => shop.ShopId == updatedShop.ShopId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedShop
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous thread with the new data
        /// </summary>
        /// <param name="updatedThread">The updated thread data</param>
        public static bool UpdateThread(UserMessageThreadModel updatedThread)
        {
            var result = MongoCollectionHelper.UserMessageThreads.ReplaceOne
            (
                thread => thread.MessageId == updatedThread.MessageId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedThread
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous trainer with the new data
        /// </summary>
        /// <param name="updatedTrainer">The updated trainer data</param>
        public static bool UpdateTrainer(TrainerModel updatedTrainer)
        {
            var result = MongoCollectionHelper.Trainers.ReplaceOne
            (
                trainer => trainer.TrainerId == updatedTrainer.TrainerId && trainer.GameId == updatedTrainer.GameId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedTrainer
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to replace the previous user with the new data
        /// </summary>
        /// <param name="updatedUser">The updated user data</param>
        public static bool UpdateUser(UserModel updatedUser)
        {
            var result = MongoCollectionHelper.Users.ReplaceOne
            (
                user => user.UserId == updatedUser.UserId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: updatedUser
            );

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Attempts to add a encounter using the provided document
        /// </summary>
        /// <param name="encounter">The document to add</param>
        public static (bool Result, MongoWriteError Error) TryAddSetting(SettingModel encounter)
        {
            return (TryAddDocument
            (
                () => MongoCollectionHelper.Settings.InsertOne(encounter),
                out var error
            ), error);
        }

        /// <summary>
        /// Attempts to add a game using the provided document
        /// </summary>
        /// <param name="game">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddGame(
            GameModel game,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Games.InsertOne(game),
                out error
            );
        }

        /// <summary>
        /// Attempts to add an npc using the provided document
        /// </summary>
        /// <param name="npc">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddNpc(
            NpcModel npc,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Npcs.InsertOne(npc),
                out error
            );
        }

        /// <summary>
        /// Attempts to add an shop using the provided document
        /// </summary>
        /// <param name="shop">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddShop(
            ShopModel shop,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Shops.InsertOne(shop),
                out error
            );
        }

        /// <summary>
        /// Attempts to update a pokemon's form
        /// </summary>
        /// <param name="pokemon">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryChangePokemonForm(
            PokemonModel pokemon,
            out MongoWriteError error)
        {
            if (DeletePokemon(pokemon.PokemonId))
            {
                return TryAddPokemon(pokemon, out error);
            }

            throw new Exception();
        }

        /// <summary>
        /// Attempts to add a Pokemon using the provided document
        /// </summary>
        /// <param name="pokemon">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddPokemon(
            PokemonModel pokemon,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Pokemon.InsertOne(pokemon),
                out error
            );
        }

        /// <summary>
        /// Attempts to add a sprite using the provided document
        /// </summary>
        /// <param name="sprite">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddSprite(
            SpriteModel sprite,
            out MongoWriteError error)
        {
            try
            {
                MongoCollectionHelper.Sprite.InsertOne(sprite);
                error = null;
                return true;
            }
            catch (MongoWriteException exception)
            {
                error = new MongoWriteError(exception.WriteError.Details.GetValue("details").AsBsonDocument.ToString());
                return false;
            }
        }

        /// <summary>
        /// Attempts to add a message thread using the provided document
        /// </summary>
        /// <param name="thread">The thread to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddThread(
            UserMessageThreadModel thread,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.UserMessageThreads.InsertOne(thread),
                out error
            );
        }

        /// <summary>
        /// Attempts to add a trainer using the provided document
        /// </summary>
        /// <param name="trainer">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddTrainer(
            TrainerModel trainer,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Trainers.InsertOne(trainer),
                out error
            );
        }

        /// <summary>
        /// Attempts to add a user using the provided document
        /// </summary>
        /// <param name="user">The document to add</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddUser(
            UserModel user,
            out MongoWriteError error)
        {
            return TryAddDocument
            (
                () => MongoCollectionHelper.Users.InsertOne(user),
                out error
            );
        }

        /// <summary>
        /// Attempts to add a dexItem using the provided document
        /// </summary>
        /// <param name="trainerId">The pokedex's trainer id</param>
        /// <param name="gameId">The pokedex's game id</param>
        /// <param name="dexNo">The dex number</param>
        /// <param name="isSeen">Whether the pokemon was seen</param>
        /// <param name="isCaught">Whether the pokemon was caught</param>
        /// <param name="error">Any error found</param>
        public static bool TryAddDexItem(
            Guid trainerId,
            Guid gameId,
            int dexNo,
            bool isSeen,
            bool isCaught,
            out MongoWriteError error)
        {
            var dexItem = new PokeDexItemModel
            {
                TrainerId = trainerId,
                DexNo = dexNo,
                IsSeen = isSeen,
                IsCaught = isCaught,
                GameId = gameId,
            };

            return TryAddDocument
            (
                () => MongoCollectionHelper.PokeDex.InsertOne(dexItem),
                out error
            );
        }

        /// <summary>
        /// Updates the pokedex entry for a seen pokemon
        /// </summary>
        /// <param name="trainerId">The trainer's id to search with</param>
        /// <param name="gameId">The game session id</param>
        /// <param name="dexNo">The dex number for the pokemon</param>
        public static bool UpdateDexItemIsSeen(Guid trainerId, Guid gameId, int dexNo)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.PokeDex,
                dexItem => dexItem.TrainerId == trainerId && dexItem.GameId == gameId && dexItem.DexNo == dexNo,
                Builders<PokeDexItemModel>.Update.Set("IsSeen", true)
            );
        }

        /// <summary>
        /// Updates the pokedex entry for a caught pokemon
        /// </summary>
        /// <param name="trainerId">The trainer's id to search with</param>
        /// <param name="gameId">The game session id</param>
        /// <param name="dexNo">The dex number for the pokemon</param>
        public static bool UpdateDexItemIsCaught(Guid trainerId, Guid gameId, int dexNo)
        {
            var updates = Builders<PokeDexItemModel>
                .Update
                .Combine(new[]
                {
                    Builders<PokeDexItemModel>.Update.Set("IsSeen", true),
                    Builders<PokeDexItemModel>.Update.Set("IsCaught", true)
                });

            return TryUpdateDocument
            (
                MongoCollectionHelper.PokeDex,
                dexItem => dexItem.TrainerId == trainerId && dexItem.GameId == gameId && dexItem.DexNo == dexNo,
                updates
            );
        }

        /// <summary>
        /// Searches for a game, then updates the npc list
        /// </summary>
        /// <param name="gameId">The game session id</param>
        /// <param name="npcIds">The updated npc list</param>
        /// <exception cref="MongoCommandException" />
        public static bool UpdateGameNpcList(Guid gameId, IEnumerable<Guid> npcIds)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Games,
                game => game.GameId == gameId,
                Builders<GameModel>.Update.Set("NPCs", npcIds)
            );
        }

        /// <summary>
        /// Searches for a game, then updates the logs
        /// </summary>
        /// <param name="theGame">The game session</param>
        /// <param name="logs">The new logs to add</param>
        /// <exception cref="MongoCommandException" />
        public static bool UpdateGameLogs(GameModel theGame, params LogModel[] logs)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Games,
                game => game.GameId == theGame.GameId,
                Builders<GameModel>.Update.Set("Logs", theGame.Logs?.Union(logs) ?? logs)
            );
        }

        /// <summary>
        /// Searches for a game, then updates its online status
        /// </summary>
        /// <param name="gameId">The game session id</param>
        /// <param name="isOnline">The updated online status</param>
        /// <exception cref="MongoCommandException" />
        public static bool UpdateGameOnlineStatus(
            Guid gameId,
            bool isOnline)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Games,
                game => game.GameId == gameId,
                Builders<GameModel>.Update.Set("IsOnline", isOnline)
            );
        }

        /// <summary>
        /// Searches for a pokemon, then updates its trainer id
        /// </summary>
        /// <param name="pokemonId">The pokemon id</param>
        /// <param name="trainerId">The trainer id</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdatePokemonTrainerId(
            Guid pokemonId,
            Guid trainerId)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Pokemon,
                pokemon => pokemon.PokemonId == pokemonId,
                Builders<PokemonModel>.Update.Set("TrainerId", trainerId)
            );
        }
        /// <summary>
        /// Searches for a pokemon, then updates its evolvability
        /// </summary>
        /// <param name="pokemonId">The pokemon id</param>
        /// <param name="isEvolvable">Whether the pokemon is evolvable</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdatePokemonEvolvability(
            Guid pokemonId,
            bool isEvolvable)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Pokemon,
                pokemon => pokemon.PokemonId == pokemonId,
                Builders<PokemonModel>.Update.Set("CanEvolve", isEvolvable)
            );
        }

        /// <summary>
        /// Attempts to update a pokemon's hp
        /// </summary>
        /// <param name="pokemonId">The pokemon's id</param>
        /// <param name="hp">The pokemon's new hp</param>
        public static bool UpdatePokemonHP(Guid pokemonId, int hp)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Pokemon,
                pokemon => pokemon.PokemonId == pokemonId,
                Builders<PokemonModel>.Update.Set("CurrentHP", hp)
            );
        }

        /// <summary>
        /// Searches for a pokemon, then updates its location
        /// </summary>
        /// <param name="pokemonId">The pokemon id</param>
        /// <param name="isOnActiveTeam">Whether the pokemon is on the active team</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdatePokemonLocation(
            Guid pokemonId,
            bool isOnActiveTeam)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Pokemon,
                pokemon => pokemon.PokemonId == pokemonId,
                Builders<PokemonModel>.Update.Set("IsOnActiveTeam", isOnActiveTeam)
            );
        }

        /// <summary>
        /// Searches for a pokemon, then evolves it to its next stage
        /// </summary>
        /// <param name="pokemonId">The pokemon id</param>
        /// <param name="evolvedForm">The pokemon's evolution</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdatePokemonWithEvolution(
            Guid pokemonId,
            PokemonModel evolvedForm)
        {
            if (evolvedForm == null)
            {
                throw new ArgumentNullException(nameof(evolvedForm));
            }

            if (DeletePokemon(pokemonId))
            {
                return TryAddPokemon(evolvedForm, out _);
            }

            throw new Exception();
        }

        /// <summary>
        /// Searches for a trainer, then updates their honors
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        /// <param name="honors">The trainer's honors</param>
        public static bool UpdateTrainerHonors(
            Guid trainerId,
            IEnumerable<string> honors)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Trainers,
                trainer => trainer.TrainerId == trainerId,
                Builders<TrainerModel>.Update.Set("Honors", honors)
            );
        }

        /// <summary>
        /// Searches for a trainer, then updates their activity token
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="token">The new activity token</param>
        public static bool UpdateUserActivityToken(
            Guid userId,
            string token)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Users,
                user => user.UserId == userId,
                Builders<UserModel>.Update.Set("ActivityToken", token)
            );
        }

        /// <summary>
        /// Searches for a trainer, then updates their item list
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        /// <param name="gameId">The game session id</param>
        /// <param name="itemList">The updated item list</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdateTrainerItemList(
            Guid trainerId,
            Guid gameId,
            IEnumerable<ItemModel> itemList)
        {
            if (itemList == null)
            {
                throw new ArgumentNullException(nameof(itemList));
            }

            return TryUpdateDocument
            (
                MongoCollectionHelper.Trainers,
                trainer => trainer.TrainerId == trainerId && trainer.GameId == gameId,
                Builders<TrainerModel>.Update.Set("Items", itemList)
            );
        }

        /// <summary>
        /// Searches for a trainer, then updates their online status
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        /// <param name="isOnline">The updated online status</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdateTrainerOnlineStatus(
            Guid trainerId,
            bool isOnline)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Trainers,
                trainer => trainer.TrainerId == trainerId,
                TrainerStatusUpdate(isOnline)
            );
        }

        /// <summary>
        /// Searches for a trainer, then updates their online status
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="isOnline">The updated online status</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdateUserOnlineStatus(
            Guid userId,
            bool isOnline)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Users,
                user => user.UserId == userId,
                UserStatusUpdate(isOnline)
            );
        }

        /// <summary>
        /// Searches for a trainer, then updates their password
        /// </summary>
        /// <param name="trainerId">The trainer id</param>
        /// <param name="password">The updated password</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="MongoCommandException" />
        public static bool UpdateTrainerPassword(
            Guid trainerId,
            string password)
        {
            return TryUpdateDocument
            (
                MongoCollectionHelper.Trainers,
                trainer => trainer.TrainerId == trainerId,
                GetTrainerPasswordUpdate(password)
            );
        }

        private static TrainerModel FindTrainerById(
            Expression<Func<TrainerModel, bool>> searchPattern)
        {
            var trainer = MongoCollectionHelper
                .Trainers
                .Find(searchPattern)
                .SingleOrDefault();

            return trainer;
        }

        private static UpdateDefinition<TrainerModel> GetTrainerPasswordUpdate(string password)
        {
            return Builders<TrainerModel>
                .Update
                .Combine(new[]
                {
                    Builders<TrainerModel>.Update.Set("PasswordHash", EncryptionUtility.HashSecret(password)),
                    Builders<TrainerModel>.Update.Set("IsOnline", true)
                });
        }

        private static bool TryAddDocument(
            Action action,
            out MongoWriteError error)
        {
            try
            {
                action();
                error = null;
                return true;
            }
            catch (MongoWriteException exception)
            {
                error = new MongoWriteError(exception.WriteError.Details.GetValue("details").AsBsonDocument.ToString());
                return false;
            }
        }

        private static bool TryUpdateDocument<TMongoCollection>(
            IMongoCollection<TMongoCollection> collection,
            Expression<Func<TMongoCollection, bool>> filter,
            UpdateDefinition<TMongoCollection> update)
        {
            if (collection.FindOneAndUpdate(filter, update) == null)
            {
                return false;
            }

            return true;
        }

        private static UpdateDefinition<TrainerModel> TrainerStatusUpdate(bool isOnline)
        {
            if (isOnline)
            {
                return Builders<TrainerModel>.Update.Set("IsOnline", isOnline);
            }

            return Builders<TrainerModel>.Update.Combine
            (
                Builders<TrainerModel>.Update.Set("IsOnline", isOnline),
                Builders<TrainerModel>.Update.Set("ActivityToken", string.Empty)
            );
        }

        private static UpdateDefinition<UserModel> UserStatusUpdate(bool isOnline)
        {
            if (isOnline)
            {
                return Builders<UserModel>.Update.Set("IsOnline", isOnline);
            }

            return Builders<UserModel>.Update.Combine
            (
                Builders<UserModel>.Update.Set("IsOnline", isOnline),
                Builders<UserModel>.Update.Set("ActivityToken", string.Empty)
            );
        }
    }
}
