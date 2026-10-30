using System.Diagnostics;
using Govor.Markdown;

// Parser/cache microbenchmark only. Does not measure UI layout, scrolling, image decode or FPS.
string[] samples = ["Привет! Как дела? 👋", "**Привет** *мир*! [Ссылка](https://example.com)",
    "# Заголовок\n\nТекст с **акцентом**.\n\n- один\n- два\n\n![alt](https://example.com/a.png)",
    string.Concat(Enumerable.Repeat("Абзац **жирный** и `code`.\n\n", 100))];
const int count = 5_000;
foreach (var sample in samples)
{
    var cache = new MarkdownCache();
    for (var i = 0; i < 100; i++) { MarkdownParser.Parse(sample); cache.Get(sample); }
    Measure($"parse {sample.Length} chars", () => MarkdownParser.Parse(sample));
    Measure($"cache {sample.Length} chars", () => cache.Get(sample));
}
void Measure(string name, Action action)
{
    var bytes = GC.GetAllocatedBytesForCurrentThread();
    var watch = Stopwatch.StartNew();
    for (var i = 0; i < count; i++) action();
    watch.Stop();
    var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
    Console.WriteLine($"{name,-20} {watch.Elapsed.TotalMicroseconds / count,10:F3} us/op {allocated / count,10} B/op");
}
