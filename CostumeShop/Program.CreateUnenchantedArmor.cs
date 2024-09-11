using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop;

public partial class Program
{

    private readonly static Armor.TranslationMask ArmorToTemplateCopyMask = new(true)
    {
        EditorID = false,
        EnchantmentAmount = false,
        ObjectEffect = false,
        VirtualMachineAdapter = false,
        TemplateArmor = false,
    };

    private HashSet<IArmorGetter> CreateUnenchantedArmor(HashSet<IArmorGetter> enchantedArmors)
    {
        IArmorGetter armor = SelectArmorWithMostComponents(enchantedArmors);

        var newArmorEditorID = "CostumeShop_" + armor.EditorID;

        var newArmor = PatchMod.Armors.AddNew(newArmorEditorID);

        CopyArmorData(newArmor, armor, ArmorToTemplateCopyMask);

        if (newArmor.Name is not null)
            newArmor.Name.String += settings.Value.ReplicaSuffix;

        newArmor.Description = null;

        var keywords = newArmor.Keywords ??= new();

        keywords.RemoveAll(KeywordsForbiddenOnReplicas.Contains);

        keywords.Add(replicaKeyword.Value);

        SetTemplateArmor(enchantedArmors, newArmor);

        RegisterCostumeLinks(newArmor);
        return new HashSet<IArmorGetter>() { newArmor };
    }

    private static IArmorGetter SelectArmorWithMostComponents(HashSet<IArmorGetter> enchantedArmors)
    {
        return enchantedArmors.OrderBy(i => i.Armature.Count).First();
    }

    private void SetTemplateArmor(HashSet<IArmorGetter> enchantedArmors, IArmorGetter armor)
    {
        var armorLink = armor.ToLinkGetter();
        foreach (var enchantedArmor in enchantedArmors.Where(a => !a.TemplateArmor.Equals(armorLink)))
            PatchMod.Armors.GetOrAddAsOverride(enchantedArmor).TemplateArmor.SetTo(armor);
    }

    private void SetTemplateArmor(HashSet<IArmorGetter> enchantedArmors, HashSet<IArmorGetter> armors)
    {
        var targetArmor = armors.First();
        if (armors.CountGreaterThan(1))
        {
            var links = armors.Select(armor => armor.ToLinkGetter());
            foreach (var enchantedArmor in enchantedArmors.Where(enchantedArmor => !links.Contains(enchantedArmor.TemplateArmor)))
                PatchMod.Armors.GetOrAddAsOverride(enchantedArmor).TemplateArmor.SetTo(targetArmor);
        }
        else
            SetTemplateArmor(enchantedArmors, targetArmor);
    }
}
