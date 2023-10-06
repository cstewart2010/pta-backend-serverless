using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Extensions
{
    internal static class RequestExtensions
    {
        static RequestExtensions()
        {
            AuthKey = Environment.GetEnvironmentVariable("CookieKey", EnvironmentVariableTarget.Process);
        }

        internal static string AuthKey { get; }

        public static string GetJsonFromRequest(this HttpRequest request)
        {
            var jsonFile = request.Form.Files.First(file => Path.GetExtension(file.FileName).ToLower() == ".json");
            if (jsonFile.Length > 0)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var json = reader.ReadToEnd();
                reader.Close();
                return json;
            }

            return null;
        }

        public static bool IsUserGM(
            this HttpRequest request,
            Guid userId,
            Guid gameId)
        {
            var gameMaster = DatabaseUtility.FindTrainerById(userId, gameId);
            var user = DatabaseUtility.FindUserById(userId);
            var isAdmin = Enum.TryParse<UserRoleOnSite>(user.SiteRole, out var role) && role == UserRoleOnSite.SiteAdmin;
            return request.VerifyIdentity(userId) && (gameMaster?.IsGM == true || isAdmin);
        }

        public static bool VerifyIdentity(
            this HttpRequest request,
            Guid id)
        {
            var user = DatabaseUtility.FindUserById(id);
            if (user == null)
            {
                return false;
            }

            if (!(request.Headers.TryGetValue("pta-activity-token", out var accessToken)
                && user.ActivityToken == accessToken
                && EncryptionUtility.ValidateToken(accessToken)))
            {
                return false;
            }

            if (!(request.Headers.TryGetValue("pta-session-auth", out var cookie) && EncryptionUtility.VerifySecret(AuthKey, cookie)))
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> TryCompleteNpc(this HttpRequest request)
        {
            var json = await request.GetRequestBody();
            var publicNpc = PublicNpc.FromJson(json);
            var npc = publicNpc.ParseBackToModel();
           
            var result = DatabaseUtility.UpdateNpc(npc);
            return result;
        }
        public static async Task<bool> TryCompleteTrainer(this HttpRequest request, Guid trainerId, Guid gameId)
        {
            var json = await request.GetRequestBody();
            var publicTrainer = PublicTrainer.FromJson(json);
            if (!CheckRequestingTrainer(trainerId, gameId, publicTrainer))
            {
                return false;
            }
            var trainer = publicTrainer.ParseBackToModel();
            AddTrainerPokemon(publicTrainer.NewPokemon, trainer);
            SetStartingEquipmentWithOrigin(trainer);
            var result = DatabaseUtility.UpdateTrainer(trainer);
            if (result)
            {
                var game = DatabaseUtility.FindGame(trainer.GameId);
                var statsAddedLog = new LogModel(trainer.TrainerName, $"has updated stats");
                DatabaseUtility.UpdateGameLogs(game, statsAddedLog);
            }

            return result;
        }

        public static async Task<(string Username, string Password, IEnumerable<string> Errors)> GetUserCredentials(
            this HttpRequest request)
        {
            var body = await request.GetRequestBody();
            var errors = new[] { "username", "password" }
                .Select(key => body[key] != null ? null : $"Missing {key}")
                .Where(error => error != null);
            var username = (string)body["username"];
            var password = (string)body["password"];

            return (username, password, errors);
        }

        public static async Task<JToken> GetRequestBody(this HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            return JToken.Parse(json);
        }

        public static async Task<T> GetRequestBody<T>(this HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            return JToken.Parse(json).ToObject<T>();
        }

        public static IEnumerable<Guid> GetNpcIds(
            this HttpRequest request,
            out AbstractMessage error)
        {
            error = null;
            var npcIds = request.Query["npcIds"].ToString().Split(',').Select(npc => new Guid(npc));
            if (npcIds.Any())
            {
                var foundNpcs = DatabaseUtility.FindNpcs(npcIds).Select(npc => npc.NPCId).ToArray();
                if (!npcIds.Any())
                {
                    error = new InvalidQueryStringMessage()
                    {
                        InvalidParameters = new[] { "npcIds" }
                    };
                }

                return foundNpcs;
            }

            error = new InvalidQueryStringMessage()
            {
                MissingParameters = new[] { "npcIds" }
            };

            return Array.Empty<Guid>();
        }

        private static bool CheckRequestingTrainer(Guid trainerId, Guid gameId, PublicTrainer trainer)
        {
            var user = DatabaseUtility.FindUserById(trainerId);
            var requestingTrainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (Enum.TryParse<UserRoleOnSite>(user.SiteRole, true, out var result) && result == UserRoleOnSite.SiteAdmin)
            {
                return true;
            }

            if (trainer.GameId != requestingTrainer.GameId)
            {
                return false;
            }

            return trainer.TrainerId == trainerId || requestingTrainer.IsGM;
        }

        private static void AddTrainerPokemon(IEnumerable<NewPokemon> pokemon, TrainerModel trainer)
        {
            foreach (var data in pokemon.Where(data => data != null))
            {
                var nickname = data.Nickname.Length > 18 ? data.Nickname.Substring(0, 18) : data.Nickname;
                var pokemonModel = DexUtility.GetNewPokemon(data.SpeciesName, nickname, data.Form);
                pokemonModel.IsOnActiveTeam = data.IsOnActiveTeam;
                pokemonModel.OriginalTrainerId = trainer.TrainerId;
                pokemonModel.TrainerId = trainer.TrainerId;
                pokemonModel.GameId = trainer.GameId;
                pokemonModel.Pokeball = Pokeball.Basic_Ball.ToString().Replace("_", "");
                DatabaseUtility.TryAddPokemon(pokemonModel, out _);
                var game = DatabaseUtility.FindGame(trainer.GameId);
                var caughtPokemonLog = new LogModel(trainer.TrainerName, $"caught a {pokemonModel.SpeciesName} named {pokemonModel.Nickname}");
                DatabaseUtility.UpdateGameLogs(game, caughtPokemonLog);
                if (DatabaseUtility.GetPokedexItem(trainer.TrainerId, trainer.GameId, pokemonModel.DexNo) == null)
                {
                    DatabaseUtility.TryAddDexItem(trainer.TrainerId, trainer.GameId, pokemonModel.DexNo, true, true, out _);
                }
                else
                {
                    DatabaseUtility.UpdateDexItemIsCaught(trainer.TrainerId, trainer.GameId, pokemonModel.DexNo);
                }
            }
        }
        
        private static void SetStartingEquipmentWithOrigin(TrainerModel trainer)
        {
            var OriginModel = DexUtility.GetOrigin(trainer.Origin);
            trainer.Items = OriginModel.StartingEquipmentList.Select(ConvertStartingEquipment).ToList();
        }

        private static ItemModel ConvertStartingEquipment(StartingEquipment s)
        {
            BaseItemModel baseItem = s.Type switch
            {
                StartingEquipmentType.Trainer => DexUtility.GetDexEntry<BaseItemModel>(DexType.TrainerEquipment, s.Name),
                StartingEquipmentType.None => throw new ArgumentOutOfRangeException(nameof(s.Type)),
                StartingEquipmentType.Pokeball => DexUtility.GetDexEntry<BaseItemModel>(DexType.Pokeballs, s.Name),
                StartingEquipmentType.Medical => DexUtility.GetDexEntry<BaseItemModel>(DexType.MedicalItems, s.Name),
                StartingEquipmentType.Berry => DexUtility.GetDexEntry<BaseItemModel>(DexType.Berries, s.Name),
                StartingEquipmentType.Pokemon => DexUtility.GetDexEntry<BaseItemModel>(DexType.PokemonItems, s.Name),
                _ => throw new ArgumentOutOfRangeException(nameof(s.Type)),
            };
            var item = new ItemModel
            {
                Name = baseItem.Name,
                Effects = baseItem.Effects,
                Amount = s.Amount,
                Type = s.Type.ToString()
            };
            return item;
        }
    }
}
