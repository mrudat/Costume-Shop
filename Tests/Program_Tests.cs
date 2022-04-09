using CostumeShop;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
    public class Program_Tests : TestBase
    {
        public Program_Tests() : base()
        {
            AddLLSTs();
        }

        [Fact]
        public void DoingNothingAddsNoNewRecords()
        {
            var linkCache = loadOrder.ToImmutableLinkCache();

            Program program = new(loadOrder, linkCache, patchMod);

            program.Run();

            Assert.Empty(patchMod.EnumerateMajorRecords());
        }

        [Theory]
        [InlineData(1, 1, 0, 1, 1, 1, 0, 1)]
        [InlineData(1, 0, 0, 1, 1, 1, 0, 1)]
        [InlineData(1, 1, 0, 0, 1, 1, 0, 1)]
        [InlineData(1, 0, 0, 0, 1, 1, 0, 1)]
        [InlineData(0, 0, 0, 1, 0, 0, 0, 1)]
        [InlineData(0, 1, 0, 1, 0, 1, 0, 1)]
        [InlineData(0, 0, 1, 1, 0, 0, 1, 1)]
        [InlineData(0, 0, 1, 0, 0, 0, 1, 1)]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        public void TestMain(
            int addEnchantedArmor,
            int addArmor,
            int addEnchantedCostume,
            int addCostume,
            int expectEnchantedArmor,
            int expectArmor,
            int expectEnchantedCostume,
            int expectCostume)
        {
            var armature = new ArmorAddon(patchMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var costume = new Armor(patchMod, "Costume");
            costume.Race.SetTo(Skyrim.Race.DefaultRace);
            costume.BodyTemplate = new();
            costume.BodyTemplate.ArmorType = ArmorType.Clothing;
            costume.Armature.Add(armature);

            Armor.TranslationMask copyMask = new(true) { EditorID = false };

            var armor = new Armor(patchMod, "Armor");
            armor.DeepCopyIn(costume, out var copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e) throw e;
            armor.BodyTemplate!.ArmorType = ArmorType.LightArmor;

            var objectEffect = new ObjectEffect(patchMod, "Enchantment");
            masterMod.ObjectEffects.Add(objectEffect);

            var enchantedCostume = new Armor(patchMod, "EnchantedCostume");
            enchantedCostume.DeepCopyIn(costume, out copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e2) throw e2;
            enchantedCostume.ObjectEffect.SetTo(objectEffect);

            var enchantedArmor = new Armor(patchMod, "EnchantedArmor");
            enchantedArmor.DeepCopyIn(armor, out copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e3) throw e3;
            enchantedArmor.ObjectEffect.SetTo(objectEffect);

            if (addEnchantedArmor > 0)
            {
                masterMod.Armors.Add(enchantedArmor);
                if (addArmor > 0)
                    enchantedArmor.TemplateArmor.SetTo(armor);
            }

            if (addArmor > 0)
            {
                masterMod.Armors.Add(armor);
            }

            if (addEnchantedCostume > 0)
            {
                masterMod.Armors.Add(enchantedCostume);
                if (addCostume > 0)
                    enchantedCostume.TemplateArmor.SetTo(costume);
            }

            if (addCostume > 0)
                masterMod.Armors.Add(costume);

            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod).Run();

            List<IArmorGetter>? foundCostumes = null;
            List<IArmorGetter>? foundEnchantedCostumes = null;
            List<IArmorGetter>? foundArmors = null;
            List<IArmorGetter>? foundEnchantedArmors = null;

            foreach (var ARMO in loadOrder.PriorityOrder
                .Armor()
                .WinningOverrides())
            {
                if (ARMO.BodyTemplate is null) continue;
                if (ARMO.BodyTemplate.ArmorType == ArmorType.Clothing)
                {
                    if (ARMO.ObjectEffect.IsNull)
                        (foundCostumes ??= new()).Add(ARMO);
                    else
                        (foundEnchantedCostumes ??= new()).Add(ARMO);
                }
                else
                {
                    if (ARMO.ObjectEffect.IsNull)
                        (foundArmors ??= new()).Add(ARMO);
                    else
                        (foundEnchantedArmors ??= new()).Add(ARMO);
                }
            }

            if (expectCostume > 0)
                Assert.Single(foundCostumes);
            else
                Assert.Null(foundCostumes);

            if (expectEnchantedCostume > 0)
                Assert.Single(foundEnchantedCostumes);
            else
                Assert.Null(foundEnchantedCostumes);

            if (expectArmor > 0)
                Assert.Single(foundArmors);
            else
                Assert.Null(foundArmors);

            if (expectEnchantedArmor > 0)
                Assert.Single(foundEnchantedArmors);
            else
                Assert.Null(foundEnchantedArmors);
        }

 
    }
}