using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop;

public partial class Program
{
    private void CreateCostumeArmor(HashSet<IArmorGetter> armors)
    {
        var armor = GetCheapestArmor(armors);
        var newCostumeArmor = PatchMod.Armors.AddNew("CostumeShop_" + armor.EditorID);

        CopyArmorData(newCostumeArmor, armor, ArmorToClothesCopyMask);

        MarkCopiedArmorAsClothing(newCostumeArmor);

        UpdateNameForCostume(newCostumeArmor);

        newCostumeArmor.Keywords ??= new(); // Ensure keywords collection exists

        UpdateKeywordsForCostume(newCostumeArmor);

        AddRequiredKeywords(newCostumeArmor);
        UpdateStatsForCostume(newCostumeArmor);
        RegisterCostumeLinks(newCostumeArmor);
    }

    private static void MarkCopiedArmorAsClothing(Armor newArmor)
    {
        newArmor.BodyTemplate!.ArmorType = ArmorType.Clothing;
    }

    private static IArmorGetter GetCheapestArmor(HashSet<IArmorGetter> armors) => armors.OrderBy(i => i.Armature.Count).First();

    private static void CopyArmorData(IArmor target, IArmorGetter source, SkyrimMajorRecord.TranslationMask copyMask)
    {
        target.DeepCopyIn(source, out var copyError, copyMask);
        if (copyError.IsInError() && copyError.Overall is Exception e) throw e;
    }

    private void UpdateNameForCostume(IArmor armor)
    {
        if (armor.Name is null)
            return;

        var name = armor.Name.String;

        if (name?.EndsWith(settings.Value.ReplicaSuffix) == true)
            name = name[..^settings.Value.ReplicaSuffix.Length];

        armor.Name.String = name + settings.Value.CostumeSuffix;
    }

    private void UpdateKeywordsForCostume(IArmor armor)
    {
        var keywords = armor.Keywords ??= new();

        // if true increases warmth one step from Cold -> normal (no keyword) -> Warm
        var makeWarmer = settings.Value.MakeCostumeArmorWarmer;

        // iterate in reverse order as it makes removing entries faster.
        for (int i = keywords.Count - 1; i >= 0; i--)
        {
            var keyword = keywords[i];
            if (ArmorKeywordsToClothesKeywords.TryGetValue(keyword, out var newKeyword))
                keywords[i] = newKeyword;
            else if (KeywordsForbiddenOnCostumes.Contains(keyword))
                keywords.RemoveAt(i);
            else if (makeWarmer)
                if (keyword.Equals(Update.Keyword.Survival_ArmorCold))
                {
                    // Cold -> normal
                    keywords.RemoveAt(i);
                    makeWarmer = false;
                }
                else if (keyword.Equals(Update.Keyword.Survival_ArmorWarm))
                    makeWarmer = false;
        }

        // normal -> Warm
        if (makeWarmer)
            keywords.Add(Update.Keyword.Survival_ArmorWarm);
    }

    private void AddRequiredKeywords(IArmor armor)
    {
        var keywords = armor.Keywords ??= new();

        // Add rich keyword; cosplay is an expensive hobby.
        keywords.Add(Skyrim.Keyword.ClothingRich);

        // Add the specific costume keyword
        keywords.Add(costumeKeyword.Value);
    }

    private void UpdateStatsForCostume(IArmor armor)
    {
        armor.ArmorRating = 0;
        armor.Weight /= settings.Value.CostumeArmorWeightDivisor;
        armor.Value = (uint)(armor.Value / settings.Value.CostumeArmorPriceDivisor);
    }
}
