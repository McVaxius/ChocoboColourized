using System.Collections.Generic;
using System.Linq;

namespace ChocoboColourized.Core;

public static class ColorDatabase
{
    private static readonly List<ChocoboColor> Colors = new()
    {
        // Default
        new ChocoboColor(219, 180, 87,  "Desert Yellow"),

        // Whites / Greys / Black
        new ChocoboColor(228, 223, 208, "Snow White"),
        new ChocoboColor(172, 168, 162, "Ash Grey"),
        new ChocoboColor(137, 135, 132, "Goobbue Grey"),
        new ChocoboColor(101, 101, 101, "Slate Grey"),
        new ChocoboColor(72,  71,  66,  "Charcoal Grey"),
        new ChocoboColor(43,  41,  35,  "Soot Black"),

        // Pinks
        new ChocoboColor(230, 159, 150, "Rose Pink"),
        new ChocoboColor(228, 141, 105, "Salmon Pink"),
        new ChocoboColor(254, 206, 245, "Lotus Pink"),
        new ChocoboColor(220, 155, 202, "Colibri Pink"),

        // Reds
        new ChocoboColor(165, 48,  34,  "Blood Red"),
        new ChocoboColor(120, 26,  26,  "Dalamud Red"),
        new ChocoboColor(145, 59,  48,  "Rust Red"),
        new ChocoboColor(91,  23,  41,  "Rolanberry Red"),
        new ChocoboColor(173, 90,  60,  "Mesa Red"),

        // Oranges
        new ChocoboColor(204, 108, 45,  "Sunset Orange"),
        new ChocoboColor(197, 116, 36,  "Pumpkin Orange"),

        // Browns
        new ChocoboColor(142, 88,  27,  "Acorn Brown"),
        new ChocoboColor(100, 66,  22,  "Orchard Brown"),
        new ChocoboColor(61,  41,  13,  "Chestnut Brown"),
        new ChocoboColor(106, 75,  55,  "Bark Brown"),
        new ChocoboColor(110, 61,  36,  "Chocolate Brown"),
        new ChocoboColor(125, 80,  44,  "Russet Brown"),
        new ChocoboColor(100, 66,  45,  "Kobold Brown"),
        new ChocoboColor(110, 93,  63,  "Cork Brown"),
        new ChocoboColor(94,  82,  65,  "Qiqirn Brown"),
        new ChocoboColor(73,  60,  45,  "Opo-opo Brown"),
        new ChocoboColor(103, 77,  54,  "Aldgoat Brown"),
        new ChocoboColor(185, 164, 137, "Gobbiebag Brown"),
        new ChocoboColor(149, 130, 106, "Shale Brown"),
        new ChocoboColor(118, 101, 84,  "Mole Brown"),
        new ChocoboColor(79,  68,  55,  "Loam Brown"),

        // Whites / Tans
        new ChocoboColor(235, 225, 199, "Bone White"),
        new ChocoboColor(183, 163, 112, "Ul Brown"),

        // Yellows
        new ChocoboColor(228, 174, 47,  "Honey Yellow"),
        new ChocoboColor(228, 193, 53,  "Millioncorn Yellow"),
        new ChocoboColor(226, 199, 40,  "Coeurl Yellow"),
        new ChocoboColor(220, 211, 67,  "Cream Yellow"),
        new ChocoboColor(200, 196, 59,  "Halatali Yellow"),
        new ChocoboColor(183, 177, 49,  "Raisin Brown"),
        new ChocoboColor(165, 161, 47,  "Canary Yellow"),
        new ChocoboColor(145, 140, 40,  "Vanilla Yellow"),

        // Greens
        new ChocoboColor(124, 128, 34,  "Mud Green"),
        new ChocoboColor(101, 116, 48,  "Sylph Green"),
        new ChocoboColor(68,  105, 32,  "Lime Green"),
        new ChocoboColor(53,  93,  34,  "Moss Green"),
        new ChocoboColor(48,  82,  40,  "Meadow Green"),
        new ChocoboColor(62,  82,  22,  "Olive Green"),
        new ChocoboColor(61,  73,  45,  "Marsh Green"),
        new ChocoboColor(110, 158, 60,  "Apple Green"),
        new ChocoboColor(82,  147, 56,  "Cactuar Green"),
        new ChocoboColor(40,  127, 47,  "Hunter Green"),
        new ChocoboColor(58,  117, 67,  "Ochu Green"),
        new ChocoboColor(57,  110, 76,  "Adamantoise Green"),
        new ChocoboColor(86,  135, 86,  "Nophica Green"),
        new ChocoboColor(78,  115, 72,  "Deepwood Green"),
        new ChocoboColor(155, 196, 161, "Celeste Green"),
        new ChocoboColor(73,  138, 122, "Turquoise Green"),
        new ChocoboColor(52,  128, 109, "Morbol Green"),

        // Blues
        new ChocoboColor(137, 179, 189, "Ice Blue"),
        new ChocoboColor(97,  174, 218, "Sky Blue"),
        new ChocoboColor(134, 158, 170, "Seafog Blue"),
        new ChocoboColor(59,  104, 134, "Peacock Blue"),
        new ChocoboColor(44,  70,  116, "Ink Blue"),
        new ChocoboColor(58,  93,  135, "Raptor Blue"),
        new ChocoboColor(66,  87,  120, "Othard Blue"),
        new ChocoboColor(42,  64,  92,  "Storm Blue"),
        new ChocoboColor(47,  56,  81,  "Void Blue"),
        new ChocoboColor(39,  48,  103, "Royal Blue"),
        new ChocoboColor(24,  25,  55,  "Midnight Blue"),
        new ChocoboColor(55,  55,  71,  "Shadow Blue"),
        new ChocoboColor(49,  45,  87,  "Abyssal Blue"),
        new ChocoboColor(89,  97,  134, "Dragoon Blue"),
        new ChocoboColor(91,  127, 192, "Turquoise Blue"),
        new ChocoboColor(62,  99,  180, "Ceruleum Blue"),
        new ChocoboColor(47,  68,  133, "Woad Blue"),

        // Purples
        new ChocoboColor(80,  73,  136, "Regal Purple"),
        new ChocoboColor(81,  69,  96,  "Gloom Purple"),
        new ChocoboColor(50,  44,  59,  "Currant Purple"),
        new ChocoboColor(143, 100, 162, "Iris Purple"),
        new ChocoboColor(105, 66,  130, "Grape Purple"),
        new ChocoboColor(131, 105, 165, "Lilac Purple"),
        new ChocoboColor(155, 127, 189, "Lavender Purple"),
    };

    public static IReadOnlyList<ChocoboColor> AllColors => Colors;

    public static string[] AllColorNames => Colors.Select(c => c.Name).ToArray();

    public static ChocoboColor GetByName(string name)
    {
        return Colors.FirstOrDefault(c => c.Name == name);
    }

    public static ChocoboColor GetByIndex(int index)
    {
        if (index < 0 || index >= Colors.Count)
            return Colors[0];
        return Colors[index];
    }

    public static ChocoboColor FindClosestColor(ChocoboColor target)
    {
        ChocoboColor closest = Colors[0];
        double minDist = double.MaxValue;
        foreach (var color in Colors)
        {
            var dist = color.DistanceTo(target);
            if (dist < minDist)
            {
                minDist = dist;
                closest = color;
            }
        }
        return closest;
    }
}
