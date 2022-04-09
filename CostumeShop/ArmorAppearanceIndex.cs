using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop
{
    class ArmorAppearanceIndex
    {

        public static ArmorClassification Classify(IArmorGetter armor)
        {
            if (armor.BodyTemplate!.ArmorType == ArmorType.Clothing)
            {
                if (armor.ObjectEffect.IsNull)
                    return ArmorClassification.Clothing;
                else
                    return ArmorClassification.EnchantedClothing;
            }
            else
            {
                if (armor.ObjectEffect.IsNull)
                    return ArmorClassification.Armor;
                else
                    return ArmorClassification.EnchantedArmor;
            }
        }

        private readonly Dictionary<IFormLinkGetter<IArmorAddonGetter>, Dictionary<int, HashSet<ArmorAppearanceRecord>>> armorAppearanceByArmature = new();

        public readonly Dictionary<IFormLinkGetter<IArmorGetter>, ArmorAppearanceRecord> armorAppearance = new();

        public readonly List<ArmorAppearanceRecord> ArmorsWithTheSameAppearance = new();


        public void Register(IArmorGetter armor)
        {
            var armorFormLink = armor.AsLinkGetter();

            var armature = armor.Armature;

            ArmorAppearanceRecord? appearanceRecord = null;

            if (armorAppearanceByArmature.TryGetValue(armature[0], out var appearanceRecordsByCount))
                if (appearanceRecordsByCount.TryGetValue(armature.Count, out var appearanceRecords))
                    appearanceRecord = appearanceRecords.FirstOrDefault(item => armature.All(aa => item.armatures.Contains(aa)));

            if (appearanceRecord is null)
            {
                appearanceRecord = new ArmorAppearanceRecord(armature);
                ArmorsWithTheSameAppearance.Add(appearanceRecord);
                foreach (var arma in armature)
                    armorAppearanceByArmature.GetOrAdd(arma).GetOrAdd(armature.Count).Add(appearanceRecord);
            }

            appearanceRecord.Add(armor);
            armorAppearance.Add(armorFormLink, appearanceRecord);
        }

        public bool TemplateArmorAppearanceMatches(IArmorGetter armor)
        {
            if (!armorAppearance.TryGetValue(armor.TemplateArmor, out var templateArmorAppearance))
                return false;

            if (armor.Armature.All(aa => templateArmorAppearance.armatures.Contains(aa)))
                return true;

            return false;
        }

        public bool ArmorAppearanceMatches(IArmorGetter armor)
        {
            var armature = armor.Armature;

            if (!armorAppearanceByArmature.TryGetValue(armature[0], out var appearanceRecordsByCount))
                return false;

            if (!appearanceRecordsByCount.TryGetValue(armature.Count, out var appearanceRecords))
                return false;

            if (appearanceRecords.Any(aas => armature.All(aa => aas.armatures.Contains(aa))))
                return true;

            return false;
        }
    }
}
