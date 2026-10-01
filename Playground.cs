#:project Homeili

using System.Diagnostics;
using Homeili;

Fst fst = new();
fst.Insert("at");
fst.Insert("bat");
fst.Contains("x");
fst.Insert("ax");

for (;;)
{
    Console.Write(">> ");
    string? input = Console.ReadLine();
    if (input == null)
        break;

    if (input.StartsWith(":s"))
    {
        Console.WriteLine(fst);
        Console.WriteLine();
        continue;
    }

    int hitCount;
    long start;
    TimeSpan elapsed;
    if (input.StartsWith(":c "))
    {
        start = Stopwatch.GetTimestamp();
        hitCount = fst.Contains(input.Remove(0, 3)) ? 1 : 0;
        elapsed = Stopwatch.GetElapsedTime(start);
    }
    else
    {
        start = Stopwatch.GetTimestamp();
        List<string> hits = fst.PrefixSearch(input, 10);
        elapsed = Stopwatch.GetElapsedTime(start);

        foreach (string hit in hits)
            Console.WriteLine(hit);

        hitCount = hits.Count;
    }

    Console.WriteLine();
    Console.WriteLine($"{hitCount} hit(s) in {elapsed.Microseconds / 1000.0}ms");
    Console.WriteLine();
}
