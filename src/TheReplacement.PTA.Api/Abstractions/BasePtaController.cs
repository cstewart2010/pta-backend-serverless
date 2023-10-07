using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Interfaces;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Abstractions
{
    public abstract class BasePtaController
    {
        protected ILogger<BasePtaController> _logger;
        protected abstract MongoCollection Collection { get; }

        protected IDocument GetDocument(Guid id, MongoCollection collection, out IActionResult notFound)
        {
            IDocument document = collection switch
            {
                MongoCollection.Games => DatabaseUtility.FindGame(id),
                MongoCollection.Npcs => DatabaseUtility.FindNpc(id),
                MongoCollection.Pokemon => DatabaseUtility.FindPokemonById(id),
                _ => throw new ArgumentOutOfRangeException(nameof(collection)),
            };

            notFound = null;
            if (document == null)
            {
                notFound = new NotFoundObjectResult(id);
            }

            return document;
        }

        protected static (PokemonModel Pokemon, AbstractMessage Message) BuildPokemon(
            Guid trainerId,
            Guid gameId,
            WildPokemon wild)
        {
            var (pokemon, error) = BuildDefaultPokemon(wild);
            if (pokemon == null)
            {
                return (null, error);
            }

            pokemon.TrainerId = trainerId;
            pokemon.OriginalTrainerId = trainerId;
            pokemon.GameId = gameId;
            return (pokemon, null);
        }

        protected static IEnumerable<LogModel> AddItemsToTrainer(TrainerModel trainer, IEnumerable<ItemModel> items)
        {
            var itemList = trainer.Items;
            foreach (var item in items)
            {
                trainer.Items = UpdateAllItemsWithAddition
                (
                    itemList,
                    item,
                    trainer
                );
            }

            var result = DatabaseUtility.UpdateTrainer(trainer);

            if (!result)
            {
                throw new Exception();
            }

            return items.Select(item => new LogModel
            (
                user: trainer.TrainerName,
                action: $"added ({item.Amount}) {item.Name}"
            ));
        }

        protected static IEnumerable<LogModel> RemoveItemsFromTrainer(TrainerModel trainer, IEnumerable<ItemModel> items)
        {
            var itemList = trainer.Items;
            foreach (var item in items)
            {
                itemList = UpdateAllItemsWithReduction
                (
                    itemList,
                    item,
                    trainer
                );
            }

            var result = DatabaseUtility.UpdateTrainerItemList
            (
                trainer.TrainerId,
                trainer.GameId,
                itemList
            );

            if (!result)
            {
                throw new Exception();
            }

            return items.Select(item => new LogModel
            (
                user: trainer.TrainerName,
                action: $"removed ({item.Amount}) {item.Name}"
            ));
        }

        protected bool IsGameAuthenticated(
            string gamePassword,
            GameModel game,
            out IActionResult authError)
        {
            var validationPassed = true;
            if (!EncryptionUtility.VerifySecret(gamePassword, game.PasswordHash))
            {
                validationPassed = false;
            }

            authError = new UnauthorizedResult();

            return validationPassed;
        }

        protected bool IsUserAuthenticated(
            string username,
            string password,
            out ActionResult authError)
        {
            var validationPassed = true;
            var user = DatabaseUtility.FindUserByUsername(username);
            if (user == null)
            {
                validationPassed = false;
            }
            else if (!EncryptionUtility.VerifySecret(password, user.PasswordHash))
            {
                validationPassed = false;
            }

            authError = new UnauthorizedResult();

            if (validationPassed)
            {
                DatabaseUtility.UpdateUserOnlineStatus(user.UserId, true);
            }

            return validationPassed;
        }

        internal static GenericMessage GetPokemonDeletion(Guid trainerId, Guid gameId)
        {
            string message = DatabaseUtility.DeletePokemonByTrainerId(gameId, trainerId) > -1
                ? $"Successfully deleted all pokemon associated with {trainerId}"
                : $"No pokemon found for trainer {trainerId}";

            return new GenericMessage(message);
        }

        private static List<ItemModel> UpdateAllItemsWithAddition(
            List<ItemModel> itemList,
            ItemModel itemToken,
            TrainerModel trainer)
        {
            var item = trainer.Items.FirstOrDefault(item => item.Name.Equals(itemToken.Name, StringComparison.CurrentCultureIgnoreCase));
            if (item == null)
            {
                itemList.Add(itemToken);
            }
            else
            {
                itemList = trainer.Items.Select(item => UpdateItemWithAddition(item, itemToken)).ToList();
            }

            return itemList;
        }

        private static ItemModel UpdateItemWithAddition(
            ItemModel item,
            ItemModel newItem)
        {
            if (item.Name == newItem.Name)
            {
                item.Amount = item.Amount + newItem.Amount > 100
                        ? 100
                        : item.Amount + newItem.Amount;
            }

            return item;
        }

        private static List<ItemModel> UpdateAllItemsWithReduction(
            List<ItemModel> itemList,
            ItemModel itemToken,
            TrainerModel trainer)
        {
            var item = trainer.Items.FirstOrDefault(item => item.Name.Equals(itemToken.Name, StringComparison.CurrentCultureIgnoreCase));
            if ((item?.Amount ?? 0) >= itemToken.Amount)
            {
                itemList = trainer.Items
                    .Select(item => UpdateItemWithReduction(item, itemToken))
                    .Where(item => item.Amount > 0)
                    .ToList();
            }

            return itemList;
        }

        private static ItemModel UpdateItemWithReduction(
            ItemModel item,
            ItemModel newItem)
        {
            if (item.Name == newItem.Name)
            {
                item.Amount -= newItem.Amount;
            }

            return item;
        }

        private static (PokemonModel Pokemon, AbstractMessage Message) BuildDefaultPokemon(WildPokemon wild)
        {
            var random = new Random();
            if (!Enum.TryParse(wild.Gender, true, out Gender gender))
            {
                var genders = Enum.GetValues<Gender>();
                gender = genders[random.Next(genders.Length)];
            }

            if (!Enum.TryParse(wild.Nature, true, out Nature nature))
            {
                var natures = Enum.GetValues<Nature>();
                nature = natures[random.Next(1, natures.Length)];
            }

            if (!Enum.TryParse(wild.Status, true, out Status status))
            {
                status = Status.Normal;
            }

            var form = wild.Form.Replace('_', '/');
            if (string.IsNullOrWhiteSpace(form))
            {
                return (null, new GenericMessage($"Mission form in request"));
            }

            var pokemon = DexUtility.GetNewPokemon
            (
                wild.Pokemon,
                nature,
                gender,
                status,
                null,
                form
            );

            if (pokemon == null)
            {
                return (null, new GenericMessage("Failed to build pokemon"));
            }

            if (wild.ForceShiny)
            {
                pokemon.IsShiny = true;
            }
            pokemon.Pokeball = Pokeball.Basic_Ball.ToString().Replace("_", "");
            return (pokemon, null);
        }
    }
}
