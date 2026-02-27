using System;
using System.Collections.Generic;
using System.Linq;

struct TC {
    public int R, G, B;
    public TC(int r, int g, int b) { R=Math.Clamp(r,0,255); G=Math.Clamp(g,0,255); B=Math.Clamp(b,0,255); }
    public double Dist(TC o) => Math.Sqrt((R-o.R)*(R-o.R)+(G-o.G)*(G-o.G)+(B-o.B)*(B-o.B));
    public TC Add(int dr, int dg, int db) => new(R+dr, G+dg, B+db);
}

class Program {
    static (int r,int g,int b)[] M = {(5,-5,-5),(-5,5,-5),(-5,-5,5),(-5,5,5),(5,-5,5),(5,5,-5)};
    static string[] N = {"Xelphatol Apple","Mamook Pear","O'Ghomoro Berries","Doman Plum","Valfruit","Cieldalaes Pineapple"};
    static TC Ap(TC c, int f) => c.Add(M[f].r, M[f].g, M[f].b);

    static void Main() {
        Console.WriteLine("=== Chocobo Colourized Algorithm Test (with shorter-path-on-tie fix) ===\n");
        RunTest("Desert Yellow", new TC(219,180,87), "Soot Black", new TC(43,41,35));
        RunTest("Desert Yellow", new TC(219,180,87), "Snow White", new TC(228,223,208));
        RunTest("Desert Yellow", new TC(219,180,87), "Blood Red", new TC(165,48,34));
        RunTest("Desert Yellow", new TC(219,180,87), "Ink Blue", new TC(44,70,116));
        RunTest("Desert Yellow", new TC(219,180,87), "Hunter Green", new TC(40,127,47));
    }

    static void RunTest(string sn, TC s, string tn, TC t) {
        Console.WriteLine($"--- {sn} ({s.R},{s.G},{s.B}) -> {tn} ({t.R},{t.G},{t.B}) ---");
        var fruits = new List<int>();
        var cur = s;
        var cd = cur.Dist(t);
        for (int iter = 0; iter < 1000; iter++) {
            var bd = cd; int bpLen = 0; int[] bp = Array.Empty<int>();
            // Prefer shorter paths on ties (matches reference stable sort)
            for (int a=0;a<6;a++) { var c1=Ap(cur,a); var d1=c1.Dist(t);
                if(d1<bd||(d1==bd&&1<bpLen)){bd=d1;bp=new[]{a};bpLen=1;}
                for(int b=0;b<6;b++){var c2=Ap(c1,b);var d2=c2.Dist(t);
                    if(d2<bd||(d2==bd&&2<bpLen)){bd=d2;bp=new[]{a,b};bpLen=2;}
                    for(int c=0;c<6;c++){var c3=Ap(c2,c);var d3=c3.Dist(t);
                        if(d3<bd||(d3==bd&&3<bpLen)){bd=d3;bp=new[]{a,b,c};bpLen=3;}
                    }}}
            if(bp.Length==0) break;
            fruits.Add(bp[0]);
            cur=Ap(cur,bp[0]); cd=cur.Dist(t);
            if(cd<0.001) break;
        }
        var counts = new int[6];
        foreach(var f in fruits) counts[f]++;
        Console.WriteLine($"  Total: {fruits.Count} fruits");
        Console.WriteLine($"  Final: ({cur.R},{cur.G},{cur.B})  Distance: {cd:F2}");
        for(int i=0;i<6;i++) if(counts[i]>0) Console.WriteLine($"    {N[i]} x{counts[i]}");
        Console.WriteLine();
    }
}
