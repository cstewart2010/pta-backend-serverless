using TheReplacement.PTA.Api.Services.Models;

namespace MongoDbImportTool.Builders
{
    internal static class BaseItemBuilder
    {
        private static readonly string KeyItemsJson = $"{JsonHelper.CurrentDirectory}/json/key_items.min.json";
        private static readonly string MedicalItemsJson = $"{JsonHelper.CurrentDirectory}/json/medical_items.min.json";
        private static readonly string PokeballsJson = $"{JsonHelper.CurrentDirectory}/json/pokeballs.min.json";
        private static readonly string PokemonItemsJson = $"{JsonHelper.CurrentDirectory}/json/pokemon_items.min.json";
        private static readonly string TrainerEquipmentJson = $"{JsonHelper.CurrentDirectory}/json/trainer_equipment.min.json";

        public static void AddItems()
        {
            TaskHelper.RunAsyncTasks
            (
                () => DatabaseHelper.AddDocuments("KeyItems", GetItems(KeyItemsJson)),
                () => DatabaseHelper.AddDocuments("MedicalItems", GetItems(MedicalItemsJson)),
                () => DatabaseHelper.AddDocuments("Pokeballs", GetItems(PokeballsJson)),
                () => DatabaseHelper.AddDocuments("PokemonItems", GetItems(PokemonItemsJson)),
                () => DatabaseHelper.AddDocuments("TrainerEquipment", GetItems(TrainerEquipmentJson))
            );
        }

        private static IEnumerable<BaseItemModel> GetItems(string path)
        {
            foreach (var child in JsonHelper.GetToken(path))
            {
                yield return JsonHelper.BuildItem(child);
            }
        }
    }
}
