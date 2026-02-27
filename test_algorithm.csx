using ChocoboColourized.Core;

var start = new ChocoboColor(219, 180, 87, "Desert Yellow");
var target = new ChocoboColor(43, 41, 35, "Soot Black");
var calc = new ColorCalculator(3);
var result = calc.Calculate(start, target);

Console.WriteLine($"Total fruits: {result.TotalFruits}");
Console.WriteLine($"Final color: ({result.FinalColor.R}, {result.FinalColor.G}, {result.FinalColor.B})");
Console.WriteLine($"Distance: {result.FinalDistance:F2}");
Console.WriteLine($"Closest: {result.ClosestColorName}");
Console.WriteLine();
foreach (var kvp in result.FruitCounts)
    Console.WriteLine($"  {FruitData.GetDisplayName(kvp.Key)} x{kvp.Value}");
