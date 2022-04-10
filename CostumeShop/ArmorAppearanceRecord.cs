using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CostumeShop
{
    public class ArmorAppearanceRecord
    {
        public readonly ImmutableHashSet<IFormLinkGetter<IArmorAddonGetter>> armatures;

        public readonly Dictionary<ArmorClassification, HashSet<IArmorGetter>> ARMOs = new();

        public ArmorAppearanceRecord(IReadOnlyList<IFormLinkGetter<IArmorAddonGetter>> armature)
        {
            this.armatures = armature.ToImmutableHashSet();
        }

        public bool Add(IArmorGetter armor)
        {
            return ARMOs.GetOrAdd(Program.Classify(armor)).Add(armor);
        }

        public void Get2(
            out HashSet<IArmorGetter>? enchantedArmors,
            out HashSet<IArmorGetter>? armors,
            out HashSet<IArmorGetter>? enchantedClothes,
            out HashSet<IArmorGetter>? clothes)
        {
            ARMOs.TryGetValue(ArmorClassification.EnchantedArmor, out enchantedArmors);
            ARMOs.TryGetValue(ArmorClassification.Armor, out armors);
            ARMOs.TryGetValue(ArmorClassification.EnchantedClothing, out enchantedClothes);
            ARMOs.TryGetValue(ArmorClassification.Clothing, out clothes);
        }

    }
}
