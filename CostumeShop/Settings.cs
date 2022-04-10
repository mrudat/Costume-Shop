namespace CostumeShop
{
    public record Settings
    {
        public string ReplicaSuffix = " (Replica)";

        public string CostumeSuffix = " (Costume)";

        public bool MakeCostumeArmorWarmer = true;

        public short LeveledListMultipilier = 2;

        // density of steel / density of bulk cloth insulation from quick google search.
        public float CostumeArmorWeightDivisor = 8000 / 550;

        public float CostumeArmorPriceDivisor = 5;
    }
}