using System;

namespace TheReplacement.PTA.Api.Objects
{
    [Flags]
    internal enum Environments
    {
        Default = 0,
        Safari = 1,
        Rainforest = 2,
        Cave = 4,
        Taiga = 8,
        Artic = 16,
        Desert = 32,
        Urban = 64,
        Freshwater = 128,
        Beach = 256,
        Tundra = 512,
        Grassland = 1024,
        Marsh = 2048,
        NoSunlight = 4096,
        Forest = 8192,
        OnWater = 16384,
        Mountain = 32768,
        InCombat = 65536
    }
}
