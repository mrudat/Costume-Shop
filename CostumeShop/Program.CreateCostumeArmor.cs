using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop
{
    public partial class Program
    {
        private HashSet<IArmorGetter> CreateCostumeArmor(HashSet<IArmorGetter> armors)
        {
            var armor = armors.OrderBy(i => i.Armature.Count).First();

            var newArmor = PatchMod.Armors.AddNew("CostumeShop_" + armor.EditorID);

            newArmor.DeepCopyIn(armor, out var copyError, ArmorToClothesCopyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e) throw e;

            newArmor.BodyTemplate!.ArmorType = ArmorType.Clothing;

            if (newArmor.Name is not null)
            {
                // TODO there's got to be a better way.
                var name = newArmor.Name.String;
                if (name?.EndsWith(" (Replica)") == true)
                    name = name.Substring(0, name.Length - " (Replica)".Length);
                newArmor.Name.String = name + " (Costume)";
            }

            var keywords = newArmor.Keywords ??= new();

            for (int i = keywords.Count - 1; i >= 0; i--)
            {
                var keyword = keywords[i];
                if (ArmorKeywordsToClothesKeywords.TryGetValue(keyword, out var newKeyword))
                    keywords[i] = newKeyword;

                if (KeywordsForbiddenOnCostumes.Contains(keyword))
                    keywords.RemoveAt(i);
            }

            // The new costume armor contains less metal and more padding, upgrade it by one level of warmth.
            MakeWarmer(newArmor);

            // Add rich keyword; cosplay is an expensive hobby.
            keywords.Add(Skyrim.Keyword.ClothingRich);

            // Remove poor keyword, if any.
            keywords.Remove(Skyrim.Keyword.ClothingPoor);

            keywords.Add(costumeKeyword.Value);

            newArmor.ArmorRating = 0;

            newArmor.Weight /= CostumeArmorWeightFactor;
            newArmor.Value /= CostumeArmorPriceFactor;

            RegisterCostumeLinks(newArmor);
            return new HashSet<IArmorGetter>() { newArmor };
        }

        private static void MakeWarmer(Armor armor)
        {
            var keywords = armor.Keywords!;
            if (keywords.Contains(Update.Keyword.Survival_ArmorCold))
                keywords.Remove(Update.Keyword.Survival_ArmorCold);
            else
                if (!keywords.Contains(Update.Keyword.Survival_ArmorWarm))
                keywords.Add(Update.Keyword.Survival_ArmorWarm);
        }
    }
}
