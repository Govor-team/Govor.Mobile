using System.Collections.Immutable;

namespace Govor.Markdown;

[Flags]
public enum RunStyle { None = 0, Bold = 1, Italic = 2, Strike = 4, Code = 8, Highlight = 16 }
public enum BlockKind { Paragraph, Heading, Code, Quote, Rule, Image }
public sealed record MarkdownRun(string Text, RunStyle Style = RunStyle.None, string? Link = null);
public sealed record MarkdownBlock(BlockKind Kind, ImmutableArray<MarkdownRun> Runs,
    int Level = 0, string? ImageUrl = null, string? Alt = null, string? Link = null);
public sealed record MarkdownDocument(ImmutableArray<MarkdownBlock> Blocks, bool IsTruncated);

public static class MarkdownUrls
{
    public static string? Link(string? value) => Allowed(value, false);
    public static string? Image(string? value) => Allowed(value, true);
    private static string? Allowed(string? value, bool image)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        return uri.Scheme is "https" or "http" || (!image && uri.Scheme == "mailto")
            ? uri.AbsoluteUri : null;
    }
}
