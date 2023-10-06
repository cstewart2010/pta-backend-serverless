using Newtonsoft.Json.Linq;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Exceptions;
using TheReplacement.PTA.Api.Services.Models;

namespace MongoDbImportTool.Builders
{
    internal static class BasePokemonBuilder
    {
        private static readonly IEnumerable<string> FirstThreeMoves = new[]
        {
            "Move 1",
            "Move 2",
            "Move 3"
        };
        private static readonly IEnumerable<string> SecondThreeMoves = new[]
        {
            "Move 4",
            "Move 5",
            "Move 6"
        };

        private static readonly string BasePokemonJson = $"{JsonHelper.CurrentDirectory}/json/base_pokemon.min.json";

        public static void AddBasePokemon()
        {
            DexUtility.AddPokedexEntries(GetPokemon(BasePokemonJson));
        }

        private static IEnumerable<BasePokemonModel> GetPokemon(string path)
        {
            foreach (var child in JsonHelper.GetToken(path))
            {
                yield return Build(child);
            }
        }

        private static BasePokemonModel Build(JToken pokemonToken)
        {
            var pokemon = new BasePokemonModel
            {
                Name = BuildPokemonName(pokemonToken),
                BaseFormName = BuildBaseForm(pokemonToken),
                Type = JsonHelper.BuildType(pokemonToken),
                Skills = JsonHelper.BuildSkills(pokemonToken),
                PokemonStats = BuildStats(pokemonToken),
                Moves = BuildMoves(pokemonToken),
                Passives = BuildPassives(pokemonToken),
                Proficiencies = BuildProficiencies(pokemonToken),
                EggGroups = BuildEggGroups(pokemonToken),
                Habitats = BuildHabitats(pokemonToken),
                Diet = BuildDiet(pokemonToken),
                Rarity = BuildRarity(pokemonToken),
                EggHatchRate = BuildEggHatchRate(pokemonToken),
                EvolvesFrom = BuildEvolvesFrom(pokemonToken),
                Stage = BuildStage(pokemonToken),
                LegendaryStats = BuildLegendaryStats(pokemonToken),
                DexNo = JsonHelper.GetIntFromToken(pokemonToken, "DexNo"),
                NormalPortrait = JsonHelper.GetStringFromToken(pokemonToken, "NormalPortrait"),
                ShinyPortrait = JsonHelper.GetStringFromToken(pokemonToken, "ShinyPortrait"),
                Form = JsonHelper.GetStringFromToken(pokemonToken, "Form")
            };

            (pokemon.Size, pokemon.Weight) = GetSizeAndWeight(pokemonToken);
            pokemon.SpecialFormName = BuildSpecialFormName(pokemonToken, pokemon.BaseFormName);
            pokemon.GMaxMove = BuildGmaxMove(pokemonToken, pokemon.SpecialFormName);
            return pokemon;
        }

        private static string BuildBaseForm(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromTokenOrDefault(pokemonToken, "Base Form");
        }

        private static string BuildPokemonName(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromToken(pokemonToken, "Pokemon");
        }

        private static StatsModel BuildStats(JToken pokemonToken)
        {
            return new StatsModel
            {
                HP = (int)pokemonToken["HP"],
                Attack = (int)pokemonToken["Attack"],
                Defense = (int)pokemonToken["Defense"],
                SpecialAttack = (int)pokemonToken["Special Attack"],
                SpecialDefense = (int)pokemonToken["Special Defense"],
                Speed = (int)pokemonToken["Speed"],
            };
        }

        private static (string Size, string Weight) GetSizeAndWeight(JToken pokemonToken)
        {
            var size = pokemonToken["Size"]?.ToString();
            if (!JsonHelper.IsStringWithValue(size))
            {
                throw new MissingJsonPropertyException("Size");
            }

            var weight = pokemonToken["Weight"]?.ToString();
            if (!JsonHelper.IsStringWithValue(weight))
            {
                throw new MissingJsonPropertyException("Weight");
            }

            if (!Enum.TryParse(size, out Size _))
            {
                throw new InvalidJsonPropertyException
                (
                    (JProperty)pokemonToken["Size"],
                    Enum.GetNames(typeof(Size))
                );
            }

            if (!Enum.TryParse(weight, out Weight _))
            {
                throw new InvalidJsonPropertyException
                (
                    (JProperty)pokemonToken["Weight"],
                    Enum.GetNames(typeof(Weight))
                );
            }

            return (size, weight);
        }

        private static IEnumerable<string> BuildMoves(JToken pokemonToken)
        {
            return pokemonToken.Children<JProperty>()
                .Where(child => FirstThreeMoves.Contains(child.Name))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue);
        }

        private static IEnumerable<string> BuildPassives(JToken pokemonToken)
        {
            return pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Passive"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue);
        }

        private static IEnumerable<string> BuildProficiencies(JToken pokemonToken)
        {
            return pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Proficiency"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue);
        }

        private static IEnumerable<string> BuildEggGroups(JToken pokemonToken)
        {
            return pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("EggGroup"))
                .Select(child => child.Value.ToString())
                .Select(eggGroup =>
                {
                    if (!Enum.TryParse(eggGroup, true, out EggGroups group))
                    {
                        return null;
                    }

                    return group.ToString();
                })
                .Where(JsonHelper.IsStringWithValue);
        }

        private static IEnumerable<string> BuildHabitats(JToken pokemonToken)
        {
            return pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Habitat"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue);
        }

        private static string BuildDiet(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromToken(pokemonToken, "Diet");
        }

        private static string BuildRarity(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromToken(pokemonToken, "Rarity");
        }

        private static string BuildEggHatchRate(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromToken(pokemonToken, "Egg Hatch Rate");
        }

        private static string BuildSpecialFormName(
            JToken pokemonToken,
            string baseForm)
        {
            return string.IsNullOrEmpty(baseForm)
                ? string.Empty
                : JsonHelper.GetStringFromToken(pokemonToken, "Special Form Type");
        }

        private static string BuildGmaxMove(
            JToken pokemonToken,
            string specialForm)
        {
            return specialForm == "Gigantamax"
                ? JsonHelper.GetStringFromToken(pokemonToken, "Gigantamax Move")
                : string.Empty;
        }

        private static string BuildEvolvesFrom(JToken pokemonToken)
        {
            return JsonHelper.GetStringFromTokenOrDefault(pokemonToken, "Evolves From");
        }

        private static int BuildStage(JToken pokemonToken)
        {
            return JsonHelper.GetIntFromToken(pokemonToken, "Stage");
        }

        private static LegendaryStatsModel BuildLegendaryStats(JToken pokemonToken)
        {
            var hp = JsonHelper.GetStringFromTokenOrDefault(pokemonToken, "Legendary HP");
            if (string.IsNullOrEmpty(hp))
            {
                return GetNonLegendaryStats();
            }

            return new LegendaryStatsModel
            {
                HP = int.Parse(hp),
                Moves = pokemonToken.Children<JProperty>()
                .Where(child => SecondThreeMoves.Contains(child.Name))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue),
                LegendaryMoves = pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Legendary Move"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue),
                Passives = pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Legendary Passive"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue),
                Features = pokemonToken.Children<JProperty>()
                .Where(child => child.Name.StartsWith("Legendary Feature"))
                .Select(child => child.Value.ToString())
                .Where(JsonHelper.IsStringWithValue),
            };
        }
        private static LegendaryStatsModel GetNonLegendaryStats()
        {
            return new LegendaryStatsModel
            {
                Moves = Array.Empty<string>(),
                LegendaryMoves = Array.Empty<string>(),
                Passives = Array.Empty<string>(),
                Features = Array.Empty<string>()
            };
        }
    }
}
