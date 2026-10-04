namespace Govor.Mobile.Utilities;

/// <summary>Serializes refreshes; notifications received during a request trigger another pass.</summary>
public sealed class CoalescingRefresh(Func<bool, Task> refresh, Action<Exception> onError) : IDisposable
{
    private readonly object _gate = new();
    private bool _pending, _reconnect, _running, _disposed;

    public void Request(bool reconnect = false)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _pending = true;
            _reconnect |= reconnect;
            if (_running) return;
            _running = true;
        }
        _ = DrainAsync();
    }

    private async Task DrainAsync()
    {
        while (true)
        {
            // Collapse bursts such as ownership transfer (profile + two membership events).
            await Task.Delay(80).ConfigureAwait(false);
            bool reconnect;
            lock (_gate)
            {
                if (_disposed || !_pending) { _running = false; return; }
                _pending = false;
                reconnect = _reconnect;
                _reconnect = false;
            }
            try { await refresh(reconnect).ConfigureAwait(false); }
            catch (Exception ex) { onError(ex); }
        }
    }

    public void Dispose()
    {
        lock (_gate) { _disposed = true; _pending = false; }
    }
}
