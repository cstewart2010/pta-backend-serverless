using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Objects
{
    public class PublicNpc
    {
        internal static PublicNpc FromJson(JToken json)
        {
            return json.ToObject<PublicNpc>();
        }

        internal PublicNpc() { }

        internal PublicNpc(NpcModel npc)
        {
            NpcId = npc.NPCId;
            GameId = npc.GameId;  
            TrainerName = npc.TrainerName;
            Feats = npc.Feats;
            TrainerClasses = npc.TrainerClasses;
            TrainerStats = npc.TrainerStats;
            var NpcPokemon = DatabaseUtility.FindPokemonByTrainerId(npc.NPCId);
            PokemonTeam = NpcPokemon
                .Where(Pokemon => Pokemon.IsOnActiveTeam);
            Level = npc.Level;
            TrainerSkills = npc.TrainerSkills;
            Gender = npc.Gender;
            Height = npc.Height;
            Weight = npc.Weight;
            Description = npc.Description;
            Personality = npc.Personality;
            Background = npc.Background;
            Goals = npc.Goals;
            Species = npc.Species;
            Sprite = npc.Sprite;
        }

        internal NpcModel ParseBackToModel()
        {
            var npc = DatabaseUtility.FindNpc(NpcId);
            npc.TrainerName = TrainerName;
            npc.Feats = Feats;
            npc.TrainerClasses = TrainerClasses;
            npc.TrainerStats = TrainerStats;   
            npc.Level = Level;
            npc.TrainerSkills = TrainerSkills;
            npc.Gender = Gender;
            npc.Height = Height;
            npc.Weight = Weight; 
            npc.Description = Description;
            npc.Personality = Personality;
            npc.Background = Background;
            npc.Goals = Goals;
            npc.Species = Species;
            npc.Sprite = Sprite;
            return npc;
        }

        public Guid NpcId { get; set; }

        public Guid GameId { get; set; }

        public string TrainerName { get; set; }

        public IEnumerable<string> Feats { get; set; }

        public IEnumerable<string> TrainerClasses { get; set; }

        public StatsModel TrainerStats { get; set; }

        public IEnumerable<PokemonModel> PokemonTeam { get; set; }

        public int Level { get; set; }

        public IEnumerable<TrainerSkill> TrainerSkills { get; set; }

        public string Gender { get; set; }

        public int Height { get; set; }

        public int Weight { get; set; }

        public string Description { get; set; }

        public string Personality { get; set; }

        public string Background { get; set; }

        public string Goals { get; set; }

        public string Species { get; set; }

        public string Sprite { get; set; }
    }
}
