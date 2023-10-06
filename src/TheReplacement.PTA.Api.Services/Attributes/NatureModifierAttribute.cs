using TheReplacement.PTA.Api.Services.Enums;

namespace TheReplacement.PTA.Api.Services.Attributes
{
    /// <summary>
    /// Represents the container for all modifiers associated with a nature
    /// </summary>
    public class NatureModifierAttribute : Attribute
    {
        internal NatureModifierAttribute(
            Flavors likedFlavor,
            Flavors dislikedFlavor,
            int attackModifier = 0,
            int defenseModifier = 0,
            int specialAttackModifier = 0,
            int specialDefenseModifier = 0,
            int speedModifier = 0)
        {
            LikedFlavor = likedFlavor;
            DislikedFlavor = dislikedFlavor;
            AttackModifier = attackModifier;
            DefenseModifier = defenseModifier;
            SpecialAttackModifier = specialAttackModifier;
            SpecialDefenseModifier = specialDefenseModifier;
            SpeedModifier = speedModifier;
        }

        /// <summary>
        /// Returns the Liked Flavor
        /// </summary>
        public Flavors LikedFlavor { get; }

        /// <summary>
        /// Returns the Disliked Flavor
        /// </summary>
        public Flavors DislikedFlavor { get; }

        /// <summary>
        /// Returns the Attack nature modifier
        /// </summary>
        public int AttackModifier { get; }

        /// <summary>
        /// Returns the Defense nature modifier
        /// </summary>
        public int DefenseModifier { get; }

        /// <summary>
        /// Returns the SpecialAttack nature modifier
        /// </summary>
        public int SpecialAttackModifier { get; }

        /// <summary>
        /// Returns the SpecialDefense nature modifier
        /// </summary>
        public int SpecialDefenseModifier { get; }

        /// <summary>
        /// Returns the Speed nature modifier
        /// </summary>
        public int SpeedModifier { get; }
    }
}
