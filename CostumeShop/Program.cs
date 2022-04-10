using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CostumeShop
{
    public partial class Program
    {
        private readonly ILoadOrder<IModListing<ISkyrimModGetter>> LoadOrder;
        private readonly ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache;
        private readonly ISkyrimMod PatchMod;

        private readonly List<IFormLinkGetter<IArmorGetter>> NewArmorLinks = new();
        private readonly List<IFormLinkGetter<IArmorGetter>> NewCostumeLinks = new();

        private readonly Lazy<IFormLinkGetter<IKeywordGetter>> replicaKeyword;
        private readonly Lazy<IFormLinkGetter<IKeywordGetter>> costumeKeyword;

        private readonly Dictionary<IFormLinkGetter<IArmorAddonGetter>, Dictionary<int, HashSet<ArmorAppearanceRecord>>> armorAppearanceByArmature = new();

        private readonly List<ArmorAppearanceRecord> ArmorsWithTheSameAppearance = new();

        private readonly static Armor.TranslationMask ArmorToClothesCopyMask = new(true)
        {
            EditorID = false,
            ArmorRating = false,
            VirtualMachineAdapter = false
        };

        private readonly static Armor.TranslationMask ArmorToTemplateCopyMask = new(true)
        {
            EditorID = false,
            EnchantmentAmount = false,
            ObjectEffect = false,
            VirtualMachineAdapter = false
        };

        public readonly static ImmutableList<IFormLinkGetter<ILeveledItemGetter>> ClothesLeveledItemsFormLinkList = new List<IFormLinkGetter<ILeveledItemGetter>>() {
            Skyrim.LeveledItem.LItemClothesAll,
            Skyrim.LeveledItem.LItemMiscVendorClothing75,
            Skyrim.LeveledItem.LItemFineClothes50
        }.ToImmutableList();

        public readonly static ImmutableList<IFormLinkGetter<ILeveledItemGetter>> ArmorLeveledItemsFormLinkList = new List<IFormLinkGetter<ILeveledItemGetter>>() {
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

        public Program(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : this(state.LoadOrder, state.LinkCache, state.PatchMod) { }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "CostumeShop.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            new Program(state.LoadOrder, state.LinkCache, state.PatchMod).Run();
        }

        public Program(ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, ISkyrimMod patchMod)
        {
            LoadOrder = loadOrder;
            LinkCache = linkCache;
            PatchMod = patchMod;

            replicaKeyword = new(() => FindOrMakeKeyword("CostumeShop_ReplicaKeyword"));
            costumeKeyword = new(() => FindOrMakeKeyword("CostumeShop_CostumeKeyword"));
        }

        public void Run()
        {
            var ARMOs = LoadOrder
                .PriorityOrder
                .Armor()
                .WinningOverrides()
                .Where(i => i.BodyTemplate is not null)
                .Where(i => i.Race.Equals(Skyrim.Race.DefaultRace))
                .Where(i => i.Armature.Count > 0)
                .ToDictionary(x => x.AsLinkGetter());

            Console.WriteLine("Classifying armor...");
            foreach (var armor in ARMOs.Values)
                Register(armor);

            Console.WriteLine("Building missing armor...");
            foreach (var armorAppearance in ArmorsWithTheSameAppearance)
            {
                armorAppearance.Get2(out var enchantedArmors, out var armors, out var enchantedClothes, out var clothes);

                if (enchantedArmors is not null)
                {
                    if (armors is null)
                        armors = CreateUnenchantedArmor(enchantedArmors);
                    else
                    {
                        EnsureAvailable(armors);
                        SetTemplateArmor(enchantedArmors, armors);
                    }
                }

                if (enchantedClothes is not null)
                {
                    if (clothes is null)
                        clothes = CreateUnenchantedArmor(enchantedClothes);
                    else
                    {
                        EnsureAvailable(clothes);
                        SetTemplateArmor(enchantedClothes, clothes);
                    }
                }

                if (armors is not null)
                {
                    if (clothes is null)
                        clothes = CreateCostumeArmor(armors);
                    else
                        EnsureAvailable(clothes);
                }
            }

            Console.WriteLine($"Created {NewArmorLinks.Count} armor templates.");

            Console.WriteLine($"Created {NewCostumeLinks.Count} costumes.");

            if (NewCostumeLinks.Count > 0 || NewArmorLinks.Count > 0)
            {
                Console.WriteLine("Adding new items to LeveledLists...");

                AddToLeveledLists(NewCostumeLinks, "LItemMiscVendorClothing_CostumeShop", ClothesLeveledItemsFormLinkList);

                AddToLeveledLists(NewArmorLinks, "LItemMiscVendorArmor_CostumeShop", ArmorLeveledItemsFormLinkList);
            }
        }

        public void EnsureAvailable(HashSet<IArmorGetter> armors)
        {
            if (armors.Any(armor => !armor.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)))
                return;

            foreach (var armor in armors)
            {
                PatchMod.Armors.GetOrAddAsOverride(armor).MajorFlags &= ~Armor.MajorFlag.NonPlayable;
                RegisterCostumeLinks(armor);
            }
        }

        private void RegisterCostumeLinks(IArmorGetter armor)
        {
            switch (armor.BodyTemplate!.ArmorType)
            {
                case ArmorType.Clothing:
                    NewCostumeLinks.Add(armor.AsLink());
                    break;
                default:
                    NewArmorLinks.Add(armor.AsLink());
                    break;
            }
        }

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

        public IFormLinkGetter<IKeywordGetter> FindOrMakeKeyword(string EditorID) => (
            LinkCache.TryResolve<IKeywordGetter>(EditorID, out var keyword)
                ? keyword
                : PatchMod.Keywords.AddNew(EditorID)
            ).AsLink();
    }
}
