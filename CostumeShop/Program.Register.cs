using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Linq;

namespace CostumeShop
{
    partial class Program
    {
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
        }
    }
}
