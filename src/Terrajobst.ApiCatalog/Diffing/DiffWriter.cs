namespace Terrajobst.ApiCatalog;

public abstract class DiffWriter : IDisposable
{
    private bool _disposed;

    public abstract void StartDiffLine(DiffKind kind);
    public abstract void EndDiffLine();
    public abstract void StartDiffSpan(DiffKind kind);
    public abstract void EndDiffSpan();

    public abstract void Write(MarkupPart part);

    public abstract int Indent { get; set; }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
            _disposed = true;
    }
}
