using CostumeShop;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
    public class AddToLeveledLists_Tests : TestBase
    {
        public AddToLeveledLists_Tests() : base()
        {
            AddLLSTs();
        }

        [Fact]
        public void DoesNothing()
        {
            List<IFormLinkGetter<IArmorGetter>> stuff = new();

            var linkCache = loadOrder.ToImmutableLinkCache();

            Program program = new(loadOrder, linkCache, patchMod, Settings);

            program.AddToLeveledLists(stuff, "Stuff", Program.ClothesLeveledItemsFormLinkList);

            Assert.Empty(patchMod.EnumerateMajorRecords());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(500)]
        [InlineData(70000)]
        public void AddLots(int count)
        {
            List<IFormLinkGetter<IArmorGetter>> stuff = new();

            for (int i = count; i > 0; i--)
            {
                IArmorGetter armor = masterMod.Armors.AddNew($"AnArmor_{i}");
                stuff.Add(armor.AsLinkGetter());
            }

            var stuffSet = stuff.ToHashSet();

            var linkCache = loadOrder.ToImmutableLinkCache();

            Program program = new(loadOrder, linkCache, patchMod, Settings);

            program.AddToLeveledLists(stuff, "Stuff", Program.ArmorLeveledItemsFormLinkList);

            linkCache = loadOrder.ToImmutableLinkCache();

            foreach (var item in Program.ArmorLeveledItemsFormLinkList)
            {
                var found = FindAllEntires(item, linkCache);

                Assert.Equal(stuff.Count, found.Count);
                Assert.True(found.All(i => stuffSet.Contains(i)));
            }
        }

        private static HashSet<IFormLinkGetter<IArmorGetter>> FindAllEntires(IFormLinkGetter<ILeveledItemGetter> llstLink, Mutagen.Bethesda.Plugins.Cache.Internals.Implementations.ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> linkCache)
        {
            Stack<IFormLinkGetter<ILeveledItemGetter>> stack = new();
            HashSet<IFormLinkGetter<IArmorGetter>> found = new();
            HashSet<IFormLinkGetter<ILeveledItemGetter>> seen = new();

            stack.Push(llstLink);
            seen.Add(llstLink);

            while (stack.Count > 0)
            {
                llstLink = stack.Pop();
                var llst = llstLink.Resolve(linkCache);

                if (llst.Entries is null)
                    continue;

                foreach (var entry in llst.Entries)
                {
                    if (entry.Data?.Reference?.IsNull != false)
                        continue;
                    if (entry.Data.Reference.TryResolve<IArmorGetter>(linkCache, out var armor))
                        found.Add(armor.AsLinkGetter());
                    else
                        if (entry.Data.Reference.TryResolve<ILeveledItemGetter>(linkCache, out var llst2))
                        if (seen.Add(llst2.AsLinkGetter()))
                            stack.Push(llst2.AsLinkGetter());
                }
            }

            return found;
        }

    }

}