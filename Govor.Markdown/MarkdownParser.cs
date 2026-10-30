using System.Collections.Immutable;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Govor.Markdown;

/// <summary>Immutable render plans; never caches native controls, gestures or binding contexts.</summary>
public static class MarkdownParser
{
    public const int MaxCharacters = 32_768;
    public const int MaxBlocks = 256;
    public const int MaxRuns = 2048;
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml().UseEmphasisExtras().UseAutoLinks().Build();

    public static MarkdownDocument Parse(string? markdown)
    {
        markdown ??= "";
        var truncated = markdown.Length > MaxCharacters;
        if (truncated)
        {
            var length = MaxCharacters;
            if (char.IsHighSurrogate(markdown[length - 1])) length--;
            markdown = markdown[..length];
        }
        var builder = new PlanBuilder();
        builder.Blocks(Markdig.Markdown.Parse(markdown, Pipeline));
        return new(builder.Result.ToImmutableArray(), truncated || builder.Truncated);
    }

    private sealed class PlanBuilder
    {
        public readonly List<MarkdownBlock> Result = [];
        public bool Truncated;
        private int _runs;
        private readonly List<MarkdownRun> _pending = [];

        private void Add(MarkdownBlock block)
        {
            if (Result.Count < MaxBlocks) Result.Add(block);
            else Truncated = true;
        }

        public void Blocks(ContainerBlock blocks, bool quote = false, string prefix = "")
        {
            foreach (var block in blocks)
            {
                if (Result.Count >= MaxBlocks || _runs >= MaxRuns) { Truncated = true; break; }
                switch (block)
                {
                    case ListBlock list:
                        var number = int.TryParse(list.OrderedStart, out var start) ? start : 1;
                        foreach (var item in list.OfType<ListItemBlock>())
                            Blocks(item, quote, prefix + (list.IsOrdered ? $"{number++}. " : "• "));
                        break;
                    case QuoteBlock quoted: Blocks(quoted, true, prefix); break;
                    case ThematicBreakBlock: Add(new(BlockKind.Rule, [])); break;
                    case CodeBlock code:
                        Add(new(BlockKind.Code, [new(code.Lines.ToString(), RunStyle.Code)]));
                        _runs++;
                        break;
                    case LeafBlock leaf when leaf.Inline is not null:
                        var kind = leaf is HeadingBlock ? BlockKind.Heading : quote ? BlockKind.Quote : BlockKind.Paragraph;
                        var level = leaf is HeadingBlock heading ? heading.Level : 0;
                        _pending.Clear();
                        if (prefix.Length > 0) Run(prefix, RunStyle.None, null);
                        Inlines(leaf.Inline, RunStyle.None, null, kind, level);
                        Flush(kind, level);
                        break;
                    case ContainerBlock container: Blocks(container, quote, prefix); break;
                }
            }
        }

        private void Flush(BlockKind kind, int level)
        {
            if (_pending.Count == 0) return;
            Add(new(kind, _pending.ToImmutableArray(), level));
            _pending.Clear();
        }

        private void Run(string text, RunStyle style, string? link)
        {
            if (text.Length == 0) return;
            if (_runs >= MaxRuns) { Truncated = true; return; }
            _runs++;
            if (_pending.Count > 0 && _pending[^1].Style == style && _pending[^1].Link == link)
                _pending[^1] = _pending[^1] with { Text = _pending[^1].Text + text };
            else _pending.Add(new(text, style, link));
        }

        private void Inlines(ContainerInline parent, RunStyle style, string? link, BlockKind kind, int level)
        {
            foreach (var inline in parent)
            {
                if (_runs >= MaxRuns || Result.Count >= MaxBlocks) { Truncated = true; return; }
                switch (inline)
                {
                    case LiteralInline literal: Run(literal.Content.ToString(), style, link); break;
                    case LineBreakInline: Run("\n", style, link); break;
                    case CodeInline code: Run(code.Content, style | RunStyle.Code, link); break;
                    case AutolinkInline auto:
                        Run(auto.Url, style, MarkdownUrls.Link(auto.IsEmail ? "mailto:" + auto.Url : auto.Url)); break;
                    case LinkInline image when image.IsImage:
                        var alt = image.FirstChild is LiteralInline literalAlt ? literalAlt.Content.ToString() : "Image";
                        var url = MarkdownUrls.Image(image.Url);
                        if (url is null) Run(alt, style, link);
                        else { Flush(kind, level); Add(new(BlockKind.Image, [], ImageUrl: url, Alt: alt, Link: link)); }
                        break;
                    case LinkInline anchor:
                        Inlines(anchor, style, MarkdownUrls.Link(anchor.Url), kind, level); break;
                    case EmphasisInline emphasis:
                        var extra = emphasis.DelimiterChar switch
                        {
                            '~' => RunStyle.Strike,
                            '=' => RunStyle.Highlight,
                            '*' or '_' => emphasis.DelimiterCount >= 2 ? RunStyle.Bold : RunStyle.Italic,
                            _ => RunStyle.None
                        };
                        Inlines(emphasis, style | extra, link, kind, level); break;
                    case ContainerInline container: Inlines(container, style, link, kind, level); break;
                }
            }
        }
    }
}
