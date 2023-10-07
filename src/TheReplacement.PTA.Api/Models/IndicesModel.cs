using Microsoft.AspNetCore.Http;
using System.IO;
using System.Reflection;

namespace TheReplacement.PTA.Api.Models
{
    internal class IndicesModel
    {
        private const string ApiVersion = "v1";
        public IndicesModel(HttpRequest request)
        {
            var hostUrl = $"{request.Scheme}://{request.Host}{request.Path}".Replace("GetIndicies", "").Trim('/');
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Pokedex = $"{hostUrl}/{ApiVersion}/pokedex";
            Berrydex = $"{hostUrl}/{ApiVersion}/berrydex";
            Featuredex = new[]
            {
                    $"{hostUrl}/{ApiVersion}/featuredex/general",
                    $"{hostUrl}/{ApiVersion}/featuredex/legendary",
                    $"{hostUrl}/{ApiVersion}/featuredex/passives",
                    $"{hostUrl}/{ApiVersion}/featuredex/skills",
                };
            Itemdex = new[]
            {
                    $"{hostUrl}/{ApiVersion}/itemdex/key",
                    $"{hostUrl}/{ApiVersion}/itemdex/medical",
                    $"{hostUrl}/{ApiVersion}/itemdex/pokeball",
                    $"{hostUrl}/{ApiVersion}/itemdex/pokemon",
                    $"{hostUrl}/{ApiVersion}/itemdex/trainer",
                };
            Movedex = $"{hostUrl}/{ApiVersion}/movedex";
            Origindex = $"{hostUrl}/{ApiVersion}/origindex";
            Classdex = $"{hostUrl}/{ApiVersion}/classdex";
        }

        public string Version { get; }
        public string Pokedex { get; }
        public string Berrydex { get; }
        public string[] Featuredex { get; }
        public string[] Itemdex { get; }
        public string Movedex { get; }
        public string Origindex { get; }
        public string Classdex { get; }
    }
}
