using TheReplacement.PTA.Api.Services.Attributes;

namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a container for valid Pokemon natures
    /// </summary>
    public enum Nature
    {
        /// <summary>
        /// Represents no valid nature
        /// </summary>
        None,

        /// <summary>
        /// Represents a Lonely nature (Attack: 2, Defense: -2)
        /// </summary>
        [NatureModifier(Flavors.Spicy, Flavors.Sour, attackModifier: 2, defenseModifier: -2)]
        Lonely,

        /// <summary>
        /// Represents a Brave nature (Attack: 2, Speed: -2)
        /// </summary>
        [NatureModifier(Flavors.Spicy, Flavors.Dry, attackModifier: 2, speedModifier: -2)]
        Brave,

        /// <summary>
        /// Represents a Adamant nature (Attack: 2, SpecialAttack: -2)
        /// </summary>
        [NatureModifier(Flavors.Spicy, Flavors.Bitter, attackModifier: 2, specialAttackModifier: -2)]
        Adamant,

        /// <summary>
        /// Represents a Naughty nature (Attack: 2, SpecialDefense: -2)
        /// </summary>
        [NatureModifier(Flavors.Spicy, Flavors.Sweet, attackModifier: 2, specialDefenseModifier: -2)]
        Naughty,

        /// <summary>
        /// Represents a Bold nature (Defense: 2, Attack: -2)
        /// </summary>
        [NatureModifier(Flavors.Sour, Flavors.Spicy, defenseModifier: 2, attackModifier: -2)]
        Bold,

        /// <summary>
        /// Represents a Relaxed nature (Defense: 2, Speed: -2)
        /// </summary>
        [NatureModifier(Flavors.Sour, Flavors.Dry, defenseModifier: 2, speedModifier: -2)]
        Relaxed,

        /// <summary>
        /// Represents a Impish nature (Defense: 2, SpecialAttack: -2)
        /// </summary>
        [NatureModifier(Flavors.Sour, Flavors.Bitter, defenseModifier: 2, specialAttackModifier: -2)]
        Impish,

        /// <summary>
        /// Represents a Lax nature (Defense: 2, SpecialDefense: -2)
        /// </summary>
        [NatureModifier(Flavors.Sour, Flavors.Sweet, defenseModifier: 2, specialDefenseModifier: -2)]
        Lax,

        /// <summary>
        /// Represents a Timid nature (Speed: 2, Attack: -2)
        /// </summary>
        [NatureModifier(Flavors.Dry, Flavors.Spicy, speedModifier: 2, attackModifier: -2)]
        Timid,

        /// <summary>
        /// Represents a Hasty nature (Speed: 2, Defense: -2)
        /// </summary>
        [NatureModifier(Flavors.Dry, Flavors.Sour, speedModifier: 2, defenseModifier: -2)]
        Hasty,

        /// <summary>
        /// Represents a Jolly nature (Speed: 2, SpecialAttack: -2)
        /// </summary>
        [NatureModifier(Flavors.Dry, Flavors.Bitter, speedModifier: 2, specialAttackModifier: -2)]
        Jolly,

        /// <summary>
        /// Represents a Naive nature (Speed: 2, SpecialDefense: -2)
        /// </summary>
        [NatureModifier(Flavors.Dry, Flavors.Sweet, speedModifier: 2, specialDefenseModifier: -2)]
        Naive,

        /// <summary>
        /// Represents a Modest nature (SpecialAttack: 2, Attack: -2)
        /// </summary>
        [NatureModifier(Flavors.Bitter, Flavors.Spicy, specialAttackModifier: 2, attackModifier: -2)]
        Modest,

        /// <summary>
        /// Represents a Mild nature (SpecialAttack: 2, Defense: -2)
        /// </summary>
        [NatureModifier(Flavors.Bitter, Flavors.Sour, specialAttackModifier: 2, defenseModifier: -2)]
        Mild,

        /// <summary>
        /// Represents a Quiet nature (SpecialAttack: 2, Speed: -2)
        /// </summary>
        [NatureModifier(Flavors.Bitter, Flavors.Dry, specialAttackModifier: 2, speedModifier: -2)]
        Quiet,

        /// <summary>
        /// Represents a Rash nature (SpecialAttack: 2, SpecialDefense: -2)
        /// </summary>
        [NatureModifier(Flavors.Bitter, Flavors.Sweet, specialAttackModifier: 2, specialDefenseModifier: -2)]
        Rash,

        /// <summary>
        /// Represents a Calm nature (SpecialDefense: 2, Attack: -2)
        /// </summary>
        [NatureModifier(Flavors.Sweet, Flavors.Spicy, specialDefenseModifier: 2, attackModifier: -2)]
        Calm,

        /// <summary>
        /// Represents a Gentle nature (SpecialDefense: 2, Defense: -2)
        /// </summary>
        [NatureModifier(Flavors.Sweet, Flavors.Sour, specialDefenseModifier: 2, defenseModifier: -2)]
        Gentle,

        /// <summary>
        /// Represents a Sassy nature (SpecialDefense: 2, Speed: -2)
        /// </summary>
        [NatureModifier(Flavors.Sweet, Flavors.Dry, specialDefenseModifier: 2, speedModifier: -2)]
        Sassy,

        /// <summary>
        /// Represents a Careful nature (SpecialDefense: 2, SpecialAttack: -2)
        /// </summary>
        [NatureModifier(Flavors.Sweet, Flavors.Bitter, specialDefenseModifier: 2, specialAttackModifier: -2)]
        Careful,
    }
}
