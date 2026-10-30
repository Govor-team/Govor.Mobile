namespace Govor.Markdown;

/// <summary>Thread-safe LRU bounded by both entries and source characters. Clear on account logout.</summary>
public sealed class MarkdownCache(int capacity = 256, int characterBudget = 1_048_576)
{
    public static MarkdownCache Shared { get; } = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<(string Text, MarkdownDocument Plan)>> _items = new(StringComparer.Ordinal);
    private readonly LinkedList<(string Text, MarkdownDocument Plan)> _lru = new();
    private int _characters;
    public int Count { get { lock (_gate) return _items.Count; } }
    public MarkdownDocument Get(string? text)
    {
        text ??= "";
        if (TryGet(text, out var plan)) return plan!;
        plan = MarkdownParser.Parse(text);
        if (capacity <= 0 || text.Length > characterBudget || text.Length > MarkdownParser.MaxCharacters) return plan;
        lock (_gate)
        {
            if (_items.TryGetValue(text, out var existing)) return existing.Value.Plan;
            while (_items.Count >= capacity || _characters + text.Length > characterBudget)
            {
                var last = _lru.Last!;
                _characters -= last.Value.Text.Length;
                _items.Remove(last.Value.Text);
                _lru.RemoveLast();
            }
            _items.Add(text, _lru.AddFirst((text, plan)));
            _characters += text.Length;
        }
        return plan;
    }
    public bool TryGet(string text, out MarkdownDocument? plan)
    {
        lock (_gate)
        {
            if (_items.TryGetValue(text, out var node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                plan = node.Value.Plan;
                return true;
            }
        }
        plan = null;
        return false;
    }
    public void Clear()
    {
        lock (_gate) { _items.Clear(); _lru.Clear(); _characters = 0; }
    }
}
