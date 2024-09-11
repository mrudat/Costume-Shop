using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace CostumeShop;

public partial class Program
{
    public void AddToLeveledLists(
        IReadOnlyList<IFormLinkGetter<IItemGetter>> itemLinkList,
        string LeveledListBaseEditorID,
        FrozenSet<IFormLinkGetter<ILeveledItemGetter>> targetLeveledItemsFormLinkList)
    {
        if (itemLinkList.Count == 0)
            return;

        var leveledItems = PatchMod.LeveledItems;

        ILeveledItem newLeveledItems = CreateAndAddToLeveledLists(leveledItems, targetLeveledItemsFormLinkList, LeveledListBaseEditorID);

        int subListIndex = 0;

        Func<LeveledItem> CreateNewSubList(IList<IFormLinkGetter<IItemGetter>> subLists)
        {
            return () =>
            {
                var newSubList = leveledItems.AddNew(LeveledListBaseEditorID + "_" + subListIndex++);
                subLists.Add(newSubList.ToLink());
                return newSubList;
            };
        }

        while (itemLinkList.Count > MAXIMUM_ITEMS_IN_A_LEVELED_LIST)
        {
            var newItemLinkList = new List<IFormLinkGetter<IItemGetter>>();

            CreateNewSublistsFromItems(itemLinkList, newItemLinkList, CreateNewSubList(newItemLinkList));

            itemLinkList = newItemLinkList;
        }

        newLeveledItems.Entries = new();

        foreach (var itemLink in itemLinkList)
            AddLeveledItemEntry(newLeveledItems.Entries, itemLink);
    }

    private ILeveledItem CreateAndAddToLeveledLists(
        SkyrimGroup<LeveledItem> leveledItems,
        FrozenSet<IFormLinkGetter<ILeveledItemGetter>> targetLeveledItemsFormLinkList,
        string LeveledListBaseEditorID)
    {
        ILeveledItem newLeveledItems = leveledItems.AddNew(LeveledListBaseEditorID);

        newLeveledItems.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

        AddToExistingLeveledLists(leveledItems, targetLeveledItemsFormLinkList, newLeveledItems);

        return newLeveledItems;
    }

    private void AddToExistingLeveledLists(
        SkyrimGroup<LeveledItem> leveledItems,
        FrozenSet<IFormLinkGetter<ILeveledItemGetter>> targetLeveledItemsFormLinkList,
        ILeveledItem newLeveledItems)
    {
        LeveledItemEntry item = new()
        {
            Data = new()
            {
                Count = settings.Value.LeveledListMultiplier,
                Level = 1,
                Reference = newLeveledItems.ToLink(),
            }
        };

        foreach (var targetLeveledItemsFormLink in targetLeveledItemsFormLinkList)
        {
            AddToExistingLeveledList(leveledItems, targetLeveledItemsFormLink, item);
        }
    }

    private void AddToExistingLeveledList(
        SkyrimGroup<LeveledItem> leveledItems,
        IFormLinkGetter<ILeveledItemGetter> targetLeveledItemsFormLink,
        LeveledItemEntry item)
    {
        ILeveledItem targetLeveledItem = leveledItems.GetOrAddAsOverride(targetLeveledItemsFormLink.Resolve(LinkCache));
        targetLeveledItem.Entries ??= new();
        targetLeveledItem.Entries.Add(item);
    }

    const int MAXIMUM_ITEMS_IN_A_LEVELED_LIST = 255;

    private static void CreateNewSublistsFromItems(
        IEnumerable<IFormLinkGetter<IItemGetter>> itemLinkList,
        IList<IFormLinkGetter<IItemGetter>> subLists,
        Func<ILeveledItem> CreateNewSubList)
    {
        var chunks = itemLinkList
                    .Select((itemLink, index) => (Index: index, ItemLink: itemLink))
                    .GroupBy(x => x.Index / MAXIMUM_ITEMS_IN_A_LEVELED_LIST)
                    .Select(group => group.Select(y => y.ItemLink).ToList())
                    .ToList();

        foreach (var chunk in chunks.SkipLast(1))
        {
            CreateNewSublistFromItems(chunk, CreateNewSubList);
        }

        var lastChunk = chunks[^1];

        if (EnoughSpaceToAddChunkToSublists(subLists, lastChunk))
        {
            subLists.AddRange(lastChunk);
        }
        else
        {
            CreateNewSublistFromItems(lastChunk, CreateNewSubList);
        }
    }

    private static bool EnoughSpaceToAddChunkToSublists(IList<IFormLinkGetter<IItemGetter>> subLists, IList<IFormLinkGetter<IItemGetter>> chunk) => (chunk.Count + subLists.Count) < MAXIMUM_ITEMS_IN_A_LEVELED_LIST;

    private static void CreateNewSublistFromItems(
        IList<IFormLinkGetter<IItemGetter>> itemLinkList,
        Func<ILeveledItem> CreateNewSubList)
    {
        var newSubList = CreateNewSubList();

        newSubList.Entries = new();

        newSubList.Flags = LeveledItem.Flag.CalculateForEachItemInCount | LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;

        foreach (var itemLink in itemLinkList)
            AddLeveledItemEntry(newSubList.Entries, itemLink);
    }

    private static void AddLeveledItemEntry(ExtendedList<LeveledItemEntry> entries, IFormLinkGetter<IItemGetter> itemLink)
    {
        LeveledItemEntryData leveledItemEntryData = new()
        {
            Count = 1,
            Level = 1
        };
        leveledItemEntryData.Reference.SetTo(itemLink);
        entries.Add(new()
        {
            Data = leveledItemEntryData
        });
    }
}
