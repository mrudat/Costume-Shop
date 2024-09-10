namespace CostumeShop;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "TODO: check if this is required by Synthesis")]
public record Settings
{
    public string ReplicaSuffix = " (Replica)";

    public string CostumeSuffix = " (Costume)";

    public bool MakeCostumeArmorWarmer = true;

    public short LeveledListMultiplier = 2;

    // density of steel / density of bulk cloth insulation from quick Google search.
    public float CostumeArmorWeightDivisor = (float)8000 / 550;

    public float CostumeArmorPriceDivisor = 5;
}