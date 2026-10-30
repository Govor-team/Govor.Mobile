using NUnit.Framework;

namespace Govor.Markdown.Tests;

public class MarkdownTests
{
    [Test]
    public void EmptyAndNullInputHaveNoBlocks()
    {
        Assert.That(MarkdownParser.Parse(null).Blocks, Is.Empty);
        Assert.That(MarkdownParser.Parse("").Blocks, Is.Empty);
    }

    [Test]
    public void EscapesReferenceLinksAndSetextHeadingsWork()
    {
        var plan = MarkdownParser.Parse("Heading\n===\n\n\\*literal\\* [link][id]\n\n[id]: https://example.com/path");
        Assert.That(plan.Blocks[0].Kind, Is.EqualTo(BlockKind.Heading));
        Assert.That(plan.Blocks[1].Runs.Any(r => r.Text.Contains("*literal*") && r.Style == RunStyle.None), Is.True);
        Assert.That(plan.Blocks[1].Runs.Any(r => r.Link == "https://example.com/path"), Is.True);
    }

    [Test]
    public void CharacterLimitDoesNotSplitEmoji()
    {
        var input = new string('a', MarkdownParser.MaxCharacters - 1) + "👋";
        var plan = MarkdownParser.Parse(input);
        Assert.That(plan.IsTruncated, Is.True);
        var text = plan.Blocks[0].Runs[0].Text;
        Assert.That(text.Length, Is.EqualTo(MarkdownParser.MaxCharacters - 1));
        Assert.That(char.IsSurrogate(text[^1]), Is.False);
    }

    [Test]
    public void PlainMessageHasOneBlockAndOneRun()
    {
        var plan = MarkdownParser.Parse("Привет 👋\nКак дела?");
        Assert.That(plan.Blocks, Has.Length.EqualTo(1));
        Assert.That(plan.Blocks[0].Runs, Has.Length.EqualTo(1));
        Assert.That(plan.Blocks[0].Runs[0].Text, Is.EqualTo("Привет 👋\nКак дела?"));
    }

    [Test]
    public void NestedStylesLinksAndCodeArePreserved()
    {
        var runs = MarkdownParser.Parse("**жирный *курсив*** ~~нет~~ ==важно== `a<b` [ссылка](https://example.com)").Blocks[0].Runs;
        Assert.That(runs.Any(r => r.Style.HasFlag(RunStyle.Bold | RunStyle.Italic)), Is.True);
        Assert.That(runs.Any(r => r.Style.HasFlag(RunStyle.Strike)), Is.True);
        Assert.That(runs.Any(r => r.Style.HasFlag(RunStyle.Highlight)), Is.True);
        Assert.That(runs.Any(r => r.Text == "a<b" && r.Style == RunStyle.Code), Is.True);
        Assert.That(runs.Any(r => r.Link == "https://example.com/"), Is.True);
    }

    [Test]
    public void BlockOrderSurvivesInlineAndLinkedImages()
    {
        var blocks = MarkdownParser.Parse("# Заголовок\n\nдо [![alt](https://example.com/a.png)](https://example.com) после\n\n> цитата\n\n```cs\na();\n```\n\n---").Blocks;
        Assert.That(blocks.Select(b => b.Kind), Is.EqualTo(new[] { BlockKind.Heading, BlockKind.Paragraph,
            BlockKind.Image, BlockKind.Paragraph, BlockKind.Quote, BlockKind.Code, BlockKind.Rule }));
        Assert.That(blocks[2].Alt, Is.EqualTo("alt"));
        Assert.That(blocks[2].Link, Is.EqualTo("https://example.com/"));
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("file:///etc/passwd")]
    [TestCase("data:text/html,abc")]
    [TestCase("relative.png")]
    public void UnsafeUrlsAreNotInteractive(string url)
    {
        Assert.That(MarkdownUrls.Link(url), Is.Null);
        Assert.That(MarkdownUrls.Image(url), Is.Null);
    }

    [Test]
    public void EmailAndAutoLinksWorkButMailImagesDoNot()
    {
        var runs = MarkdownParser.Parse("https://example.com <test@example.com>").Blocks[0].Runs;
        Assert.That(runs.Count(r => r.Link is not null), Is.EqualTo(2));
        Assert.That(MarkdownUrls.Image("mailto:test@example.com"), Is.Null);
    }

    [Test]
    public void HtmlIsLiteralAndImagesWithInvalidUrlsKeepAltText()
    {
        var text = string.Concat(MarkdownParser.Parse("<script>alert(1)</script> ![alt](file:///tmp/a)").Blocks.SelectMany(b => b.Runs).Select(r => r.Text));
        Assert.That(text, Does.Contain("<script>"));
        Assert.That(text, Does.Contain("alt"));
    }

    [Test]
    public void ListsIncludeNumbersAndNestedBullets()
    {
        var blocks = MarkdownParser.Parse("3. three\n4. four\n   - nested").Blocks;
        Assert.That(blocks[0].Runs[0].Text, Does.StartWith("3. "));
        Assert.That(blocks[1].Runs[0].Text, Does.StartWith("4. "));
        Assert.That(blocks.Any(b => b.Runs.Any(r => r.Text.Contains("• nested"))), Is.True);
    }

    [Test]
    public void LimitsBoundUntrustedDocuments()
    {
        Assert.That(MarkdownParser.Parse(new string('x', 40_000)).IsTruncated, Is.True);
        var blocks = MarkdownParser.Parse(string.Concat(Enumerable.Repeat("x\n\n", 400)));
        Assert.That(blocks.IsTruncated, Is.True);
        Assert.That(blocks.Blocks.Length, Is.LessThanOrEqualTo(MarkdownParser.MaxBlocks));
        var runs = MarkdownParser.Parse(string.Concat(Enumerable.Repeat("*a* ", 4000)));
        Assert.That(runs.IsTruncated, Is.True);
        Assert.That(runs.Blocks.Sum(b => b.Runs.Length), Is.LessThanOrEqualTo(MarkdownParser.MaxRuns));
    }

    [Test]
    public void LruEvictsLeastRecentlyUsedAndOversizeBypassesCache()
    {
        var cache = new MarkdownCache(2, 10);
        var a = cache.Get("a");
        cache.Get("b");
        Assert.That(cache.Get("a"), Is.SameAs(a));
        cache.Get("c");
        Assert.That(cache.TryGet("b", out _), Is.False);
        cache.Get(new string('z', 11));
        Assert.That(cache.Count, Is.EqualTo(2));
        cache.Get("1234567890");
        Assert.That(cache.Count, Is.EqualTo(1));
        cache.Clear();
        Assert.That(cache.Count, Is.Zero);
    }

    [Test]
    public void ConcurrentReadsDoNotShareMutableStateOrExceedCapacity()
    {
        var cache = new MarkdownCache(4);
        Parallel.For(0, 1000, i => cache.Get($"**{i % 8}**"));
        Assert.That(cache.Count, Is.LessThanOrEqualTo(4));
        var plans = new MarkdownDocument[50];
        Parallel.For(0, plans.Length, i => plans[i] = cache.Get("same"));
        Assert.That(plans.All(p => ReferenceEquals(p, plans[0])), Is.True);
    }
}
