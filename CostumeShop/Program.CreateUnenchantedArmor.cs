using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop
{
    public partial class Program
    {

        private HashSet<IArmorGetter> CreateUnenchantedArmor(HashSet<IArmorGetter> enchantedArmors)
        {
            var armor = enchantedArmors.OrderBy(i => i.Armature.Count).First();

            var newArmorEditorID = "CostumeShop_" + armor.EditorID;

            var newArmor = PatchMod.Armors.AddNew(newArmorEditorID);
            if (armor.BodyTemplate!.ArmorType == ArmorType.Clothing)
                NewCostumeLinks.Add(newArmor.AsLink());
            else
                NewArmorLinks.Add(newArmor.AsLink());

            newArmor.DeepCopyIn(armor, out var copyError, ArmorToTemplateCopyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e) throw e;

            if (newArmor.Name is not null)
                newArmor.Name.String += " (Replica)";

            newArmor.Description?.Clear();

            var keywords = newArmor.Keywords ??= new();

            for (int i = keywords.Count - 1; i >= 0; i--)
                if (KeywordsForbiddenOnReplicas.Contains(keywords[i]))
                    keywords.RemoveAt(i);

            keywords.Add(replicaKeyword.Value);

            foreach (var enchantedArmor in enchantedArmors)
                PatchMod.Armors.GetOrAddAsOverride(enchantedArmor).TemplateArmor.SetTo(newArmor);

            return new HashSet<IArmorGetter>() { newArmor };
        }
    }
}
