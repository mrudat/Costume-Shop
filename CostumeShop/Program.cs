using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CostumeShop
{
    public class Program
    {
        private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> state;

        private readonly static Armor.TranslationMask ArmorToClothesCopyMask = new(true)
        {
            EditorID = false,
            ArmorRating = false
        };

        private readonly static Armor.TranslationMask ArmorToTemplateCopyMask = new(true)
        {
            EditorID = false,
            EnchantmentAmount = false,
            ObjectEffect = false
        };

        private readonly static ImmutableList<IFormLinkGetter<ILeveledItemGetter>> ClothesLeveledItemsFormLinkList = new List<IFormLinkGetter<ILeveledItemGetter>>() {
            Skyrim.LeveledItem.LItemClothesAll,
            Skyrim.LeveledItem.LItemMiscVendorClothing75,
            Skyrim.LeveledItem.LItemFineClothes50
        }.ToImmutableList();

        private readonly static ImmutableList<IFormLinkGetter<ILeveledItemGetter>> ArmorLeveledItemsFormLinkList = new List<IFormLinkGetter<ILeveledItemGetter>>() {
            Skyrim.LeveledItem.LItemBlacksmithArmor75
        }.ToImmutableList();

        private readonly static ImmutableDictionary<IFormLinkGetter<IKeywordGetter>, IFormLinkGetter<IKeywordGetter>> ArmorKeywordsToClothesKeywords = new Dictionary<IFormLinkGetter<IKeywordGetter>, IFormLinkGetter<IKeywordGetter>>()
        {
            { Skyrim.Keyword.ArmorBoots, Skyrim.Keyword.ClothingFeet },
            { Skyrim.Keyword.ArmorCuirass, Skyrim.Keyword.ClothingBody },
            { Skyrim.Keyword.ArmorGauntlets, Skyrim.Keyword.ClothingHands },
            { Skyrim.Keyword.ArmorHelmet, Skyrim.Keyword.ClothingHead },
            { Skyrim.Keyword.VendorItemArmor, Skyrim.Keyword.VendorItemClothing },
            { Skyrim.Keyword.ArmorHeavy, Skyrim.Keyword.ArmorClothing },
            { Skyrim.Keyword.ArmorLight, Skyrim.Keyword.ArmorClothing },
        }.ToImmutableDictionary();

        private readonly static ImmutableList<IFormLinkGetter<IKeywordGetter>> KeywordsForbiddenOnReplicas = new List<IFormLinkGetter<IKeywordGetter>>(){
            Skyrim.Keyword.MagicDisallowEnchanting,
            Skyrim.Keyword.VendorNoSale
        }.ToImmutableList();

        private readonly static ImmutableList<IFormLinkGetter<IKeywordGetter>> KeywordsForbiddenOnCostumes = new List<IFormLinkGetter<IKeywordGetter>>(){
            Skyrim.Keyword.PerkFistsDaedric,
            Skyrim.Keyword.PerkFistsDragonplate,
            Skyrim.Keyword.PerkFistsDwarven,
            Skyrim.Keyword.PerkFistsEbony,
            Skyrim.Keyword.PerkFistsIron,
            Skyrim.Keyword.PerkFistsOrcish,
            Skyrim.Keyword.PerkFistsSteel,
            Skyrim.Keyword.PerkFistsSteelPlate,
        }.ToImmutableList();

        private readonly static float CostumeArmorWeightFactor = 8000 / 550; // density of steel / density of bulk cloth insulation from quick google search.

        private readonly static uint CostumeArmorPriceFactor = 5;

        public Program(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            this.state = state;
        }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "CostumeShop.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            new Program(state).Run();
        }

        private void Run()
        {
            var ARMOs = state
                .LoadOrder
                .PriorityOrder
                .Armor()
                .WinningOverrides()
                .Where(i => i.Armature.Count > 0)
                .Where(i => i.Race.FormKey == Skyrim.Race.DefaultRace.FormKey)
                .ToDictionary(x => x.FormKey);

            var templateARMOLinks = new HashSet<IFormLinkGetter<IArmorGetter>>();
            var unenchantedARMOs = new HashSet<IArmorGetter>();
            var enchantedARMOsWithNoTemplate = new HashSet<IArmorGetter>();

            Console.WriteLine("Classifying armor...");
            foreach (var armor in ARMOs.Values)
            {
                if (armor.ObjectEffect.IsNull)
                    unenchantedARMOs.Add(armor);
                else
                {
                    if ((!armor.TemplateArmor.IsNull) && ARMOs.TryGetValue(armor.TemplateArmor.FormKey, out var templateArmor) && templateArmor.ObjectEffect.IsNull)
                        templateARMOLinks.Add(armor.TemplateArmor);
                    else
                        enchantedARMOsWithNoTemplate.Add(armor);
                }
            }

            var templateARMOs = templateARMOLinks.Select(i => ARMOs[i.FormKey]).ToHashSet();

            HashSet<IFormLinkGetter<IArmorAddonGetter>> armatures = new();

            Console.WriteLine("Marking template armors as playable...");
            foreach (var armor in templateARMOs)
            {
                if (armor.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable))
                {
                    state.PatchMod.Armors.GetOrAddAsOverride(armor).MajorFlags &= ~Armor.MajorFlag.NonPlayable;
                    // TODO add to LLSTs?
                }

                armatures.Add(armor.Armature[0]);
            }

            Console.WriteLine("Creating missing template (unenchaned) armor...");

            List<IFormLink<Armor>> newArmorLinks = new();

            foreach (var item in from armor in enchantedARMOsWithNoTemplate
                                 where !armatures.Contains(armor.Armature[0])
                                 group armor by armor.Armature[0])
            {

                var armor = item.OrderBy(i => i.Armature.Count).First();

                var newArmorEditorID = "CostumeShop_" + ((ISkyrimMajorRecordGetter)(item.Count() == 1 ? armor : item.Key.Resolve(state.LinkCache))).EditorID;

                var newArmor = state.PatchMod.Armors.AddNew(newArmorEditorID);
                newArmorLinks.Add(newArmor.AsLink());
                templateARMOs.Add(newArmor);

                newArmor.DeepCopyIn(armor, out var foo, ArmorToTemplateCopyMask);
                if (foo.IsInError() && foo.Overall is Exception e) throw e;

                if (newArmor.Name is not null)
                    newArmor.Name.String += " (Replica)";

                newArmor.Description?.Clear();

                var keywords = newArmor.Keywords;

                if (keywords is not null)
                    for (int i = keywords.Count - 1; i >= 0; i--)
                        if (KeywordsForbiddenOnReplicas.Contains(keywords[i]))
                            keywords.RemoveAt(i);

                // I suspect that there is no need to reduce price as the final price factors in the enchantment.
                foreach (var enchantedArmor in enchantedArmorWithNoTemplate)
                    state.PatchMod.Armors.GetOrAddAsOverride(enchantedArmor).TemplateArmor.SetTo(newArmor);
            }

            Console.WriteLine($"Created {newArmorLinks.Count} armor templates.");

            List<IFormLink<Armor>> newCostumeLinks = new();

            Console.WriteLine("Creating costume (unarmored) variants of template armor...");
            foreach (var armor in templateARMOs)
            {
                if (armor.BodyTemplate is null) continue;
                if (armor.BodyTemplate.ArmorType is ArmorType.Clothing) continue;

                var newArmor = state.PatchMod.Armors.AddNew("CostumeShop_" + armor.EditorID);

                newCostumeLinks.Add(newArmor.AsLink());

                newArmor.DeepCopyIn(armor, out var foo, ArmorToClothesCopyMask);
                if (foo.IsInError() && foo.Overall is Exception e) throw e;

                newArmor.BodyTemplate!.ArmorType = ArmorType.Clothing;

                if (newArmor.Name is not null)
                    newArmor.Name.String += " (Costume)";

                var keywords = newArmor.Keywords ??= new();

                for (int i = keywords.Count - 1; i >= 0; i--) {
                    var keyword = keywords[i];
                    if (ArmorKeywordsToClothesKeywords.TryGetValue(keyword, out var newKeyword))
                        keywords[i] = newKeyword;

                    if (KeywordsForbiddenOnCostumes.Contains(keyword))
                        keywords.RemoveAt(i);
                }

                // The new costume armor contains less metal and more padding, upgrade it by one level of warmth.
                if (keywords.Contains(Update.Keyword.Survival_ArmorCold))
                    keywords.Remove(Update.Keyword.Survival_ArmorCold);
                else
                    if (!keywords.Contains(Update.Keyword.Survival_ArmorWarm))
                        keywords.Add(Update.Keyword.Survival_ArmorWarm);

                // Add rich keyword; cosplay is an expensive hobby.
                keywords.Add(Skyrim.Keyword.ClothingRich);

                // Remove poor keyword, if any.
                keywords.Remove(Skyrim.Keyword.ClothingPoor);

                newArmor.ArmorRating = 0;

                newArmor.Weight /= CostumeArmorWeightFactor;
                newArmor.Value /= CostumeArmorPriceFactor;
            }

            Console.WriteLine($"Created {newCostumeLinks.Count} costumes.");

            if (newCostumeLinks.Count > 0 || newArmorLinks.Count > 0)
            {
                Console.WriteLine("Adding new items to LeveledLists...");

                AddToLeveledLists(newCostumeLinks, "LItemMiscVendorClothing_CostumeShop", ClothesLeveledItemsFormLinkList);

                AddToLeveledLists(newArmorLinks, "LItemMiscVendorArmor_CostumeShop", ArmorLeveledItemsFormLinkList);
            }

            // TODO create recipes
        }

        private void AddToLeveledLists(List<IFormLink<Armor>> newArmorLinkList, string LeveledListBaseEditorID, IList<IFormLinkGetter<ILeveledItemGetter>> targetLeveledItemsFormLinkList)
        {
            if (newArmorLinkList.Count == 0)
                return;

            var leveledItems = state.PatchMod.LeveledItems;

            LeveledItem newLeveledItems = leveledItems.AddNew(LeveledListBaseEditorID);

            foreach (var targetLeveledItemsFormLink in targetLeveledItemsFormLinkList)
                (leveledItems.GetOrAddAsOverride(targetLeveledItemsFormLink.Resolve(state.LinkCache)).Entries ??= new()).Add(new()
                {
                    Data = new()
                    {
                        Count = 1,
                        Level = 1,
                        Reference = newLeveledItems.AsLink(),
                    }
                });

            newLeveledItems.Entries = new();

            newLeveledItems.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

            if (newArmorLinkList.Count > 255)
            {
                int subListCounter = 0;

                List<IFormLink<IItemGetter>> subLists = new();

                void AddThingsToThing(IEnumerable<IFormLink<IItemGetter>> subList)
                {
                    LeveledItem newSubList = leveledItems.AddNew(LeveledListBaseEditorID + "_" + subListCounter++);

                    subLists.Add(newSubList.AsLink());

                    newSubList.Entries = new();

                    newSubList.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

                    foreach (var newCostumeLink in subList)
                        newSubList.Entries.Add(new()
                        {
                            Data = new()
                            {
                                Count = 1,
                                Level = 1,
                                Reference = newCostumeLink
                            }
                        });
                }

                foreach (var chunk in newArmorLinkList
                    .Select((x, i) => (Index: i, Value: x))
                    .GroupBy(x => x.Index / 255)
                    .Select(x => x.Select(y => y.Value)))
                    if ((chunk.Count() + subLists.Count) < 255)
                        subLists.AddRange(chunk);
                    else
                        AddThingsToThing(chunk);

                while (subLists.Count > 255)
                {
                    var oldSubLists = subLists;
                    subLists = new();

                    foreach (var chunk in oldSubLists
                        .Select((x, i) => (Index: i, Value: x))
                        .GroupBy(x => x.Index / 255)
                        .Select(x => x.Select(y => y.Value)))
                        if ((chunk.Count() + subLists.Count) < 255)
                            subLists.AddRange(chunk);
                        else
                            AddThingsToThing(chunk);
                }

                foreach (var subList in subLists)
                    newLeveledItems.Entries.Add(new()
                    {
                        Data = new()
                        {
                            Count = 1,
                            Level = 1,
                            Reference = subList
                        }
                    });
            }
            else
                foreach (var newCostumeLink in newArmorLinkList)
                    newLeveledItems.Entries.Add(new()
                    {
                        Data = new()
                        {
                            Count = 1,
                            Level = 1,
                            Reference = newCostumeLink,
                        }
                    });
        }
    }
}
