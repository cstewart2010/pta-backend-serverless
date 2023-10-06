using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using TheReplacement.PTA.Api.Services.Internal;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services
{
    /// <summary>
    /// Provides a collection of methods to handle import/exports of game sessions
    /// </summary>
    public static class ExportUtility
    {
        /// <summary>
        /// Returns a file stream for the a json file of the game session
        /// </summary>
        /// <param name="game">The game session to export</param>
        public static FileStream GetExportStream(GameModel game)
        {
            var (path, json) = GetStreamParts(game);
            using var writer = new StreamWriter(path);
            writer.Write(json);
            writer.Close();
            return new FileStream(path, FileMode.Open);
        }

        /// <summary>
        /// Return true if the game session was successfully imported
        /// </summary>
        /// <param name="json">The stringified json object to parse</param>
        /// <param name="errors">The errors found while attempting to import the game session</param>
        /// <returns></returns>
        public static bool TryParseImport(
            string json,
            out List<string> errors)
        {
            var import = GetParsedImportFromExport(json, out errors);
            return !errors.Any() && CleanyAddGame(import, out errors);
        }

        private static ExportedGame GetParsedImportFromExport(
            string json,
            out List<string> errors)
        {
            JSchema schema = JSchema.Parse(File.ReadAllText("./ExportedGame.schema.json"));
            var jsonObject = JObject.Parse(json);
            errors = new List<string>(); ;
            if (!jsonObject.IsValid(schema, out IList<string> errorMessages))
            {
                errors.AddRange(errorMessages);
                return null;
            }

            return jsonObject.ToObject<ExportedGame>();
        }

        private static bool CleanyAddGame(
            ExportedGame import,
            out List<string> errors)
        {
            errors = new List<string>();
            if (!TryAddGame(import, errors))
            {
                return false;
            }

            var gameId = import.GameSession.GameId;
            foreach (var trainer in import.Trainers)
            {
                CleanlyAddTrainer(trainer, gameId, out var trainerErrors);
                errors.AddRange(trainerErrors);
            }

            return true;
        }

        private static bool TryAddGame(
            ExportedGame import,
            List<string> errors)
        {
            var game = import.GameSession;
            if (import.Trainers.SingleOrDefault(import => import.Trainer.IsGM) == null)
            {
                errors.Add($"Exactly one gm should be listed for game {game.GameId}");
                return false;
            }

            if (DatabaseUtility.FindGame(game.GameId) != null)
            {
                errors.Add($"Found extant ame session found with id {game.GameId}");
                return false;
            }

            if (game.Logs == null)
            {
                game.Logs = Array.Empty<LogModel>();
            }
            game.Logs = game.Logs.Append(new LogModel
            (
                user:"Import Tool",
                action: $"Recreated game {game.GameId}"
            ));
            if (!DatabaseUtility.TryAddGame(game, out var error))
            {
                errors.Add(error.WriteErrorJsonString);
                return false;
            }

            return true;
        }

        private static void CleanlyAddTrainer(
            ExportedTrainer import,
            Guid gameId,
            out List<string> errors)
        {
            errors = new List<string>();
            var trainer = import.Trainer;
            if (!(trainer.GameId == gameId && DatabaseUtility.TryAddTrainer(trainer, out _)))
            {
                errors.Add($"Failed to import trainer {trainer.TrainerId}");
                return;
            }

            errors = import.Pokemon
                .Select(pokemon => AddPokemon(pokemon, trainer.TrainerId))
                .Where(error => !string.IsNullOrEmpty(error))
                .ToList();
        }

        private static string AddPokemon(
            PokemonModel pokemon,
            Guid trainerId)
        {
            if (pokemon.TrainerId == trainerId)
            {
                if (DatabaseUtility.TryAddPokemon(pokemon, out _))
                {
                    return null;
                }

                return $"Failed to import pokemon {pokemon.PokemonId}";
            }

            return $"Invalid trainer id from pokemon {pokemon.PokemonId}. Skipping...";
        }

        private static (string Path, string Json) GetStreamParts(GameModel game)
        {
            game.IsOnline = false;
            var exportedGame = new ExportedGame(game);
            return (Path.GetTempFileName(), JsonConvert.SerializeObject(exportedGame));
        }
    }
}
