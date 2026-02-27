using System.Collections.Generic;

namespace ChocoboColourized.Core;

public enum FruitType
{
    XelphatolApple,
    MamookPear,
    OGhomoroBerries,
    DomanPlum,
    Valfruit,
    CieldalaesPineapple
}

public struct FruitModifier
{
    public int R { get; }
    public int G { get; }
    public int B { get; }

    public FruitModifier(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}

public static class FruitData
{
    public static readonly FruitType[] AllFruits =
    {
        FruitType.XelphatolApple,
        FruitType.MamookPear,
        FruitType.OGhomoroBerries,
        FruitType.DomanPlum,
        FruitType.Valfruit,
        FruitType.CieldalaesPineapple
    };

    private static readonly Dictionary<FruitType, FruitModifier> Modifiers = new()
    {
        { FruitType.XelphatolApple,       new FruitModifier(+5, -5, -5) },
        { FruitType.DomanPlum,            new FruitModifier(-5, +5, +5) },
        { FruitType.MamookPear,           new FruitModifier(-5, +5, -5) },
        { FruitType.Valfruit,             new FruitModifier(+5, -5, +5) },
        { FruitType.OGhomoroBerries,      new FruitModifier(-5, -5, +5) },
        { FruitType.CieldalaesPineapple,  new FruitModifier(+5, +5, -5) },
    };

    private static readonly Dictionary<FruitType, string> DisplayNames = new()
    {
        { FruitType.XelphatolApple,       "Xelphatol Apple" },
        { FruitType.DomanPlum,            "Doman Plum" },
        { FruitType.MamookPear,           "Mamook Pear" },
        { FruitType.Valfruit,             "Valfruit" },
        { FruitType.OGhomoroBerries,      "O'Ghomoro Berries" },
        { FruitType.CieldalaesPineapple,  "Cieldalaes Pineapple" },
    };

    // FFXIV item IDs for inventory lookups (verified from xivapi/ffxiv-datamining Item.csv)
    // 8157 = Xelphatol Apple, 8158 = Doman Plum, 8159 = Mamook Pear
    // 8160 = Valfruit, 8161 = O'Ghomoro Berries, 8162 = Cieldalaes Pineapple
    private static readonly Dictionary<FruitType, uint> ItemIds = new()
    {
        { FruitType.XelphatolApple,       8157 },
        { FruitType.DomanPlum,            8158 },
        { FruitType.MamookPear,           8159 },
        { FruitType.Valfruit,             8160 },
        { FruitType.OGhomoroBerries,      8161 },
        { FruitType.CieldalaesPineapple,  8162 },
    };

    public static FruitModifier GetModifier(FruitType fruit) => Modifiers[fruit];

    public static string GetDisplayName(FruitType fruit) => DisplayNames[fruit];

    public static uint GetItemId(FruitType fruit) => ItemIds[fruit];
}
