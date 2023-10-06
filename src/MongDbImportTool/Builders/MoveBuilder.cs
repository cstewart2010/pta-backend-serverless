using Newtonsoft.Json.Linq;
using TheReplacement.PTA.Api.Services.Models;

namespace MongoDbImportTool.Builders
{
    internal static class MoveBuilder
    {
        private static readonly string MovesJson = $"{JsonHelper.CurrentDirectory}/json/moves.min.json";

        public static void AddMoves()
        {
            DatabaseHelper.AddDocuments("Moves", GetMoves(MovesJson));
        }

        private static IEnumerable<MoveModel> GetMoves(string path)
        {
            foreach (var child in JsonHelper.GetToken(path))
            {
                yield return Build(child);
            }
        }

        private static MoveModel Build(JToken moveToken)
        {
            return new MoveModel
            {
                Name = JsonHelper.GetNameFromToken(moveToken),
                Range = JsonHelper.GetStringFromToken(moveToken, "Range"),
                Type = JsonHelper.BuildType(moveToken),
                Stat = JsonHelper.GetStringFromToken(moveToken, "Stat"),
                Frequency = JsonHelper.GetStringFromToken(moveToken, "Frequency"),
                DiceRoll = JsonHelper.GetStringFromToken(moveToken, "DiceRoll"),
                Effects = JsonHelper.GetEffectsFromToken(moveToken),
                GrantedSkills = JsonHelper.BuildSkills(moveToken),
                ContestKeyword = JsonHelper.GetStringFromToken(moveToken, "Contest Keyword"),
                ContestStat = JsonHelper.GetStringFromToken(moveToken, "Contest Stat")
            };
        }
    }
}
