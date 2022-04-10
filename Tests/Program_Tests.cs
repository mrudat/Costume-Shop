using CostumeShop;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Noggog;
using System;
using System.Collections.Generic;
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

            Program program = new(loadOrder, linkCache, patchMod, Settings);

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
            var armature = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var costume = new Armor(masterMod, "Costume");
            costume.Race.SetTo(Skyrim.Race.DefaultRace);
            costume.BodyTemplate ??= new();
            costume.BodyTemplate.ArmorType = ArmorType.Clothing;
            costume.Armature.Add(armature);

            Armor.TranslationMask copyMask = new(true) { EditorID = false };

            var armor = new Armor(masterMod, "Armor");
            armor.DeepCopyIn(costume, out var copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e) throw e;
            armor.BodyTemplate!.ArmorType = ArmorType.LightArmor;

            var objectEffect = new ObjectEffect(masterMod, "Enchantment");
            masterMod.ObjectEffects.Add(objectEffect);

            var enchantedCostume = new Armor(masterMod, "EnchantedCostume");
            enchantedCostume.DeepCopyIn(costume, out copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e2) throw e2;
            enchantedCostume.ObjectEffect.SetTo(objectEffect);

            var enchantedArmor = new Armor(masterMod, "EnchantedArmor");
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

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            CollectArmor(out var foundCostumes, out var foundEnchantedCostumes, out var foundArmors, out var foundEnchantedArmors);

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

        [Fact]
        public void EnchantedArmorTest()
        {
            var armature = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var objectEffect = new ObjectEffect(masterMod, "Enchantment");
            masterMod.ObjectEffects.Add(objectEffect);

            var enchantedArmor = new Armor(masterMod, "EnchantedArmor");
            enchantedArmor.Race.SetTo(Skyrim.Race.DefaultRace);
            enchantedArmor.BodyTemplate ??= new();
            enchantedArmor.BodyTemplate.ArmorType = ArmorType.LightArmor;
            enchantedArmor.Armature.Add(armature);
            enchantedArmor.ObjectEffect.SetTo(objectEffect);
            enchantedArmor.Keywords ??= new();
            enchantedArmor.Keywords.Add(Skyrim.Keyword.PerkFistsSteel);
            enchantedArmor.Keywords.Add(Skyrim.Keyword.ArmorGauntlets);
            enchantedArmor.Keywords.Add(Skyrim.Keyword.MagicDisallowEnchanting);
            enchantedArmor.Keywords.Add(Update.Keyword.Survival_ArmorCold);

            // TODO surely hard-coding this won't cause any issues...
            (enchantedArmor.Name ??= new(Language.English)).String = "An Enchanted Armor";

            (enchantedArmor.Description ??= new(Language.English)).String = "A Nifty Enchantment";

            masterMod.Armors.Add(enchantedArmor);

            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            CollectArmor(out var foundCostumes, out var foundEnchantedCostumes, out var foundArmors, out var foundEnchantedArmors);

            Assert.Null(foundEnchantedCostumes);
            Assert.Single(foundEnchantedArmors);

            var foundArmor = Assert.Single(foundArmors);

            Assert.Equal("An Enchanted Armor (Replica)", foundArmor.Name?.String);
            Assert.Null(foundArmor.Description);
            Assert.NotNull(foundArmor.Keywords);
            var armorKeywords = foundArmor.Keywords!;
            Assert.True(armorKeywords.Contains(Skyrim.Keyword.ArmorGauntlets.FormKey));
            Assert.True(armorKeywords.Contains(Skyrim.Keyword.PerkFistsSteel.FormKey));
            Assert.True(armorKeywords.Contains(Update.Keyword.Survival_ArmorCold.FormKey));
            Assert.False(armorKeywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting.FormKey));
            var foundCostume = Assert.Single(foundCostumes);

            Assert.Equal("An Enchanted Armor (Costume)", foundCostume.Name?.String);
            Assert.Null(foundCostume.Description);
            Assert.NotNull(foundCostume.Keywords);
            var costumeKeywords = foundCostume.Keywords!;
            Assert.True(costumeKeywords.Contains(Skyrim.Keyword.ClothingHands.FormKey));
            Assert.True(costumeKeywords.Contains(Skyrim.Keyword.ClothingRich.FormKey));
            Assert.False(costumeKeywords.Contains(Skyrim.Keyword.PerkFistsSteel.FormKey));
            Assert.False(costumeKeywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting.FormKey));
            Assert.False(costumeKeywords.Contains(Update.Keyword.Survival_ArmorCold.FormKey));
            Assert.False(costumeKeywords.Contains(Update.Keyword.Survival_ArmorWarm.FormKey));
        }

        [Fact]
        public void EnchantedCostumeTest()
        {
            var armature = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var objectEffect = new ObjectEffect(masterMod, "Enchantment");
            masterMod.ObjectEffects.Add(objectEffect);

            var enchantedArmor = new Armor(masterMod, "EnchantedClothing");
            enchantedArmor.Race.SetTo(Skyrim.Race.DefaultRace);
            enchantedArmor.BodyTemplate ??= new();
            enchantedArmor.BodyTemplate.ArmorType = ArmorType.Clothing;
            enchantedArmor.Armature.Add(armature);
            enchantedArmor.ObjectEffect.SetTo(objectEffect);
            enchantedArmor.Keywords ??= new();
            enchantedArmor.Keywords.Add(Skyrim.Keyword.ClothingHands);
            enchantedArmor.Keywords.Add(Skyrim.Keyword.MagicDisallowEnchanting);

            // TODO surely hard-coding this won't cause any issues...
            (enchantedArmor.Name ??= new(Language.English)).String = "Enchanted Gloves";

            (enchantedArmor.Description ??= new(Language.English)).String = "A Shifty Enchantment";

            masterMod.Armors.Add(enchantedArmor);

            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            CollectArmor(out var foundCostumes, out var foundEnchantedCostumes, out var foundArmors, out var foundEnchantedArmors);

            Assert.Null(foundArmors);
            Assert.Null(foundEnchantedArmors);
            Assert.Single(foundEnchantedCostumes);

            var foundCostume = Assert.Single(foundCostumes);

            Assert.Equal("Enchanted Gloves (Replica)", foundCostume.Name?.String);
            Assert.Null(foundCostume.Description);
            Assert.NotNull(foundCostume.Keywords);
            var costumeKeywords = foundCostume.Keywords!;
            Assert.True(costumeKeywords.Contains(Skyrim.Keyword.ClothingHands.FormKey));
            Assert.False(costumeKeywords.Contains(Update.Keyword.Survival_ArmorCold.FormKey));
            Assert.False(costumeKeywords.Contains(Update.Keyword.Survival_ArmorWarm.FormKey));
            Assert.False(costumeKeywords.Contains(Skyrim.Keyword.ClothingRich.FormKey));
            Assert.False(costumeKeywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting.FormKey));
        }

        [Fact]
        public void NonPlayableFlagCleared()
        {
            var armature = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var costume = new Armor(masterMod, "Costume");
            costume.Race.SetTo(Skyrim.Race.DefaultRace);
            costume.BodyTemplate ??= new();
            costume.BodyTemplate.ArmorType = ArmorType.Clothing;
            costume.Armature.Add(armature);

            Armor.TranslationMask copyMask = new(true) { EditorID = false };

            var armor = new Armor(masterMod, "Armor");
            armor.DeepCopyIn(costume, out var copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e) throw e;
            armor.BodyTemplate!.ArmorType = ArmorType.LightArmor;

            costume.MajorFlags |= Armor.MajorFlag.NonPlayable;

            masterMod.Armors.Add(costume);
            masterMod.Armors.Add(armor);

            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            CollectArmor(out var foundCostumes, out var foundEnchantedCostumes, out var foundArmors, out var foundEnchantedArmors);

            Assert.Null(foundEnchantedCostumes);
            Assert.Null(foundEnchantedArmors);
            Assert.Single(foundArmors);

            var foundCostume = Assert.Single(foundCostumes);

            Assert.False(foundCostume.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable));
        }

        private void CollectArmor(
            out List<IArmorGetter>? foundCostumes,
            out List<IArmorGetter>? foundEnchantedCostumes,
            out List<IArmorGetter>? foundArmors,
            out List<IArmorGetter>? foundEnchantedArmors)
        {
            foundCostumes = null;
            foundEnchantedCostumes = null;
            foundArmors = null;
            foundEnchantedArmors = null;

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
        }

        [Fact]
        public void TestMultipleTemplates()
        {
            var objectEffect = new ObjectEffect(masterMod, "Enchantment");
            masterMod.ObjectEffects.Add(objectEffect);

            var armature = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature);

            var template1 = new Armor(masterMod, "Template1");
            template1.Race.SetTo(Skyrim.Race.DefaultRace);
            template1.BodyTemplate ??= new();
            template1.BodyTemplate.ArmorType = ArmorType.Clothing;
            template1.Armature.Add(armature);
            masterMod.Armors.Add(template1);

            Armor.TranslationMask copyMask = new(true) { EditorID = false };

            var template2 = new Armor(masterMod, "Template2");
            template2.DeepCopyIn(template1, out var copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e1) throw e1;
            masterMod.Armors.Add(template2);

            var enchantedArmor1 = new Armor(masterMod, "Enchanted1");
            enchantedArmor1.DeepCopyIn(template1, out copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e2) throw e2;
            enchantedArmor1.ObjectEffect.SetTo(objectEffect);
            masterMod.Armors.Add(enchantedArmor1);

            var enchantedArmor2 = new Armor(masterMod, "Enchanted2");
            enchantedArmor2.DeepCopyIn(enchantedArmor1, out copyError, copyMask);
            if (copyError.IsInError() && copyError.Overall is Exception e3) throw e3;
            enchantedArmor2.TemplateArmor.SetTo(template1);
            masterMod.Armors.Add(enchantedArmor2);

            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            linkCache = loadOrder.ToImmutableLinkCache();

            var updatedTemplate1 = template1.AsLinkGetter().Resolve(linkCache);
            Assert.Same(template1, updatedTemplate1);

            var updatedTemplate2 = template2.AsLinkGetter().Resolve(linkCache);
            Assert.Same(template2, updatedTemplate2);

            var updatedEnchantedArmor2 = enchantedArmor2.AsLinkGetter().Resolve(linkCache);
            Assert.Same(enchantedArmor2, updatedEnchantedArmor2);

            var updatedEnchantedArmor1 = enchantedArmor1.AsLinkGetter().Resolve(linkCache);
            Assert.NotSame(enchantedArmor1, updatedEnchantedArmor1);
            Assert.False(updatedEnchantedArmor1.TemplateArmor.IsNull);
        }

        [Fact]
        public void TestMultipleARMAs()
        {
            var armature1 = new ArmorAddon(masterMod, "ArmorAA");
            masterMod.ArmorAddons.Add(armature1);

            var armature2 = new ArmorAddon(masterMod, "ArmorAA2");
            masterMod.ArmorAddons.Add(armature2);

            var armor1 = new Armor(masterMod, "Armor1");
            armor1.Race.SetTo(Skyrim.Race.DefaultRace);
            armor1.BodyTemplate ??= new();
            armor1.BodyTemplate.ArmorType = ArmorType.LightArmor;
            armor1.Armature.Add(armature1);
            masterMod.Armors.Add(armor1);

            var armor2 = new Armor(masterMod, "Armor2");
            armor2.Race.SetTo(Skyrim.Race.DefaultRace);
            armor2.BodyTemplate ??= new();
            armor2.BodyTemplate.ArmorType = ArmorType.LightArmor;
            armor2.Armature.Add(armature1);
            armor2.Armature.Add(armature2);
            masterMod.Armors.Add(armor2);


            var linkCache = loadOrder.ToImmutableLinkCache();

            new Program(loadOrder, linkCache, patchMod, Settings).Run();

            CollectArmor(out var foundCostumes, out var foundEnchantedCostumes, out var foundArmors, out var foundEnchantedArmors);

            Assert.Null(foundEnchantedArmors);
            Assert.Null(foundEnchantedCostumes);

            Assert.NotNull(foundArmors);
            Assert.NotNull(foundCostumes);

            Assert.Equal(2, foundArmors?.Count);
            Assert.Equal(2, foundCostumes?.Count);
        }
    }
}