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

        public readonly Dictionary<ArmorClassification, HashSet<IArmorGetter>> foo = new();

        public ArmorAppearanceRecord(IReadOnlyList<IFormLinkGetter<IArmorAddonGetter>> armature)
        {
            this.armatures = armature.ToImmutableHashSet();
        }

        public bool Add(IArmorGetter armor)
        {
            return foo.GetOrAdd(ArmorAppearanceIndex.Classify(armor)).Add(armor);
        }

        public HashSet<IArmorGetter>? Get(ArmorClassification classification)
        {
            foo.TryGetValue(classification, out var ret);
            return ret;
        }
    }
}
