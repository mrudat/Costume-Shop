using CostumeShop;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using Xunit;

namespace Tests
{
    public class FindOrMakeKeyword_Tests : TestBase
    {
        [Theory]
        [InlineData(true, "Fred")]
        [InlineData(false, "Fred")]
        public void TestFindOrMakeKeyword(bool addBefore, string editorID)
        {
            if (addBefore)
                masterMod.Keywords.AddNew(editorID);

            var linkCache = loadOrder.ToImmutableLinkCache();

            Program program = new(loadOrder, linkCache, patchMod);

            program.FindOrMakeKeyword(editorID);

            var afterLinkCache = loadOrder.ToImmutableLinkCache();

            afterLinkCache.TryResolve<IKeywordGetter>(editorID, out var keyword);

            Assert.NotNull(keyword);
        }
    }

}