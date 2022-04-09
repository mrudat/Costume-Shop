using CostumeShop;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace Tests
{
    public abstract class TestBase
    {
        public static readonly ModKey masterModKey = ModKey.FromNameAndExtension("Master.esm");

        public static readonly ModKey patchModKey = ModKey.FromNameAndExtension("Patch.esp");

        protected readonly SkyrimMod masterMod;
        protected readonly SkyrimMod patchMod;
        protected readonly LoadOrder<IModListing<ISkyrimModGetter>> loadOrder;

        public TestBase()
        {
            masterMod = new SkyrimMod(masterModKey, SkyrimRelease.SkyrimSE);
            patchMod = new SkyrimMod(patchModKey, SkyrimRelease.SkyrimSE);

            loadOrder = new LoadOrder<IModListing<ISkyrimModGetter>>
            {
                new ModListing<ISkyrimModGetter>(masterMod, true),
                new ModListing<ISkyrimModGetter>(patchMod, true)
            };
        }

        public void AddLLSTs()
        {
            foreach (var litem in Enumerable.Concat(Program.ClothesLeveledItemsFormLinkList, Program.ArmorLeveledItemsFormLinkList))
            {
                LeveledItem temp = new(litem.FormKey, masterMod.GameRelease.ToSkyrimRelease());
                masterMod.LeveledItems.Add(temp);
            }
        }
    }
}