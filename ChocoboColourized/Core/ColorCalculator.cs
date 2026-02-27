using System;
using System.Collections.Generic;

namespace ChocoboColourized.Core;

public class CalculationResult
{
    public List<FruitType> Fruits { get; set; } = new();
    public ChocoboColor StartColor { get; set; }
    public ChocoboColor TargetColor { get; set; }
    public ChocoboColor FinalColor { get; set; }
    public double FinalDistance { get; set; }
    public string ClosestColorName { get; set; } = "";

    public Dictionary<FruitType, int> FruitCounts
    {
        get
        {
            var counts = new Dictionary<FruitType, int>();
            foreach (var fruit in Fruits)
            {
                if (counts.ContainsKey(fruit))
                    counts[fruit]++;
                else
                    counts[fruit] = 1;
            }
            return counts;
        }
    }

    public int TotalFruits => Fruits.Count;
}

public class ColorCalculator
{
    private readonly int _lookahead;

    public ColorCalculator(int lookahead = 3)
    {
        _lookahead = Math.Clamp(lookahead, 1, 5);
    }

    public CalculationResult Calculate(ChocoboColor start, ChocoboColor target)
    {
        var result = new CalculationResult
        {
            StartColor = start,
            TargetColor = target,
        };

        var currentColor = start;
        var currentDistance = currentColor.DistanceTo(target);

        // Safety limit to prevent infinite loops
        const int maxIterations = 1000;
        var iterations = 0;

        while (iterations < maxIterations)
        {
            iterations++;

            // Find the best path with lookahead
            var bestPath = new List<FruitType>();
            var bestDistance = currentDistance;

            FindBestPath(currentColor, target, new FruitType[_lookahead], 0, ref bestPath, ref bestDistance);

            // If no path improves our position, we're done
            if (bestPath.Count == 0)
                break;

            // Add the first fruit from the best path and advance
            var nextFruit = bestPath[0];
            result.Fruits.Add(nextFruit);
            currentColor = currentColor.AddFruit(nextFruit);
            currentDistance = currentColor.DistanceTo(target);

            // If we've reached the target exactly, stop
            if (currentDistance < 0.001)
                break;
        }

        result.FinalColor = currentColor;
        result.FinalDistance = currentDistance;
        result.ClosestColorName = ColorDatabase.FindClosestColor(currentColor).Name;

        return result;
    }

    private void FindBestPath(
        ChocoboColor current,
        ChocoboColor target,
        FruitType[] path,
        int depth,
        ref List<FruitType> bestPath,
        ref double bestDistance)
    {
        foreach (var fruit in FruitData.AllFruits)
        {
            path[depth] = fruit;
            var newColor = current.AddFruit(fruit);
            var distance = newColor.DistanceTo(target);

            var pathLen = depth + 1;
            if (distance < bestDistance || (distance == bestDistance && pathLen < bestPath.Count))
            {
                bestDistance = distance;
                bestPath = new List<FruitType>();
                for (var i = 0; i <= depth; i++)
                    bestPath.Add(path[i]);
            }

            if (depth + 1 < _lookahead)
            {
                FindBestPath(newColor, target, path, depth + 1, ref bestPath, ref bestDistance);
            }
        }
    }
}
