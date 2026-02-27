using System;

namespace ChocoboColourized.Core;

public struct ChocoboColor : IEquatable<ChocoboColor>
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Name { get; set; }

    public ChocoboColor(int r, int g, int b, string name = "")
    {
        R = Clamp(r);
        G = Clamp(g);
        B = Clamp(b);
        Name = name;
    }

    public double DistanceTo(ChocoboColor other)
    {
        var dr = R - other.R;
        var dg = G - other.G;
        var db = B - other.B;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    public ChocoboColor AddFruit(FruitType fruit)
    {
        var mod = FruitData.GetModifier(fruit);
        return new ChocoboColor(R + mod.R, G + mod.G, B + mod.B);
    }

    public ChocoboColor AddFruitPath(FruitType[] path)
    {
        var result = this;
        foreach (var fruit in path)
        {
            result = result.AddFruit(fruit);
        }
        return result;
    }

    private static int Clamp(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }

    public bool Equals(ChocoboColor other) => R == other.R && G == other.G && B == other.B;
    public override bool Equals(object? obj) => obj is ChocoboColor other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(R, G, B);
    public static bool operator ==(ChocoboColor left, ChocoboColor right) => left.Equals(right);
    public static bool operator !=(ChocoboColor left, ChocoboColor right) => !left.Equals(right);

    public override string ToString() => string.IsNullOrEmpty(Name)
        ? $"({R}, {G}, {B})"
        : $"{Name} ({R}, {G}, {B})";
}
