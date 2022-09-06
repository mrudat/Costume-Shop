using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop
{
    public partial class Program
    {
        public void AddToLeveledLists(List<IFormLinkGetter<IArmorGetter>> newArmorLinkList, string LeveledListBaseEditorID, IList<IFormLinkGetter<ILeveledItemGetter>> targetLeveledItemsFormLinkList)
        {
            if (newArmorLinkList.Count == 0)
                return;

            static void AddEntry(ExtendedList<LeveledItemEntry> entries, IFormLinkGetter<IItemGetter> newCostumeLink)
            {
                LeveledItemEntryData leveledItemEntryData = new()
                {
                    Count = 1,
                    Level = 1
                };
                leveledItemEntryData.Reference.SetTo(newCostumeLink);
                entries.Add(new()
                {
                    Data = leveledItemEntryData
                });
            }

            var leveledItems = PatchMod.LeveledItems;

            LeveledItem newLeveledItems = leveledItems.AddNew(LeveledListBaseEditorID);

            foreach (var targetLeveledItemsFormLink in targetLeveledItemsFormLinkList)
                (leveledItems.GetOrAddAsOverride(targetLeveledItemsFormLink.Resolve(LinkCache)).Entries ??= new()).Add(new()
                {
                    Data = new()
                    {
                        Count = settings.Value.LeveledListMultipilier,
                        Level = 1,
                        Reference = newLeveledItems.ToLink(),
                    }
                });

            newLeveledItems.Entries = new();

            newLeveledItems.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

            if (newArmorLinkList.Count > 255)
            {
                int subListCounter = 0;

                List<IFormLinkGetter<IItemGetter>> subLists = new();

                void AddThingsToThing(IEnumerable<IFormLinkGetter<IItemGetter>> subList)
                {
                    LeveledItem newSubList = leveledItems.AddNew(LeveledListBaseEditorID + "_" + subListCounter++);

                    subLists.Add(newSubList.ToLink());

                    newSubList.Entries = new();

                    newSubList.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

                    foreach (var newCostumeLink in subList)
                        AddEntry(newSubList.Entries, newCostumeLink);
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
                    AddEntry(newLeveledItems.Entries, subList);
            }
            else
                foreach (var newCostumeLink in newArmorLinkList)
                    AddEntry(newLeveledItems.Entries, newCostumeLink);
        }
    }
}
