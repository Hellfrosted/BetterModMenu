namespace BetterModMenu.Data;

// All coordinator methods are called by the UI thread. Fetch receives only the snapshot.
internal sealed class WorkshopTagCacheCoordinator
{
    private readonly Func<IReadOnlyList<string>, Task<Dictionary<string, List<string>>>> _fetch;
    private (string ModId, string PublishedFileId)[] _installations = [];
    private Task<Dictionary<string, List<string>>>? _pending;
    private bool _attempted;
    private bool _failed;

    public WorkshopTagCacheCoordinator(Func<IReadOnlyList<string>, Task<Dictionary<string, List<string>>>> fetch)
    {
        _fetch = fetch;
    }

    public IReadOnlyDictionary<string, List<string>> Tags { get; private set; } = new Dictionary<string, List<string>>();
    public bool IsPending => _pending != null;

    public void Reset()
    {
        if (_pending != null)
            _ = _pending.ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted);
        _installations = [];
        _pending = null;
        _attempted = false;
        _failed = false;
        Tags = new Dictionary<string, List<string>>();
    }

    public void Seed(IEnumerable<(string ModId, string PublishedFileId)> installations, IReadOnlyDictionary<string, List<string>> tags)
    {
        Reset();
        _installations = SortInstallations(installations);
        Tags = new Dictionary<string, List<string>>(tags, StringComparer.Ordinal);
        _attempted = true;
    }

    public bool Request(IEnumerable<(string ModId, string PublishedFileId)> installations, bool retryFailure = false)
    {
        var snapshot = SortInstallations(installations);
        bool changed = !_installations.SequenceEqual(snapshot);
        if (changed)
        {
            Reset();
            _installations = snapshot;
        }

        if (_pending != null || (_attempted && !(retryFailure && _failed)))
            return changed;

        _attempted = true;
        _failed = false;
        if (snapshot.Length == 0)
            return changed;

        try
        {
            _pending = _fetch(snapshot.Select(item => item.PublishedFileId).Distinct(StringComparer.Ordinal).ToArray());
        }
        catch (Exception ex)
        {
            _pending = Task.FromException<Dictionary<string, List<string>>>(ex);
        }
        return changed;
    }

    private static (string ModId, string PublishedFileId)[] SortInstallations(IEnumerable<(string ModId, string PublishedFileId)> installations)
        => installations.OrderBy(item => item.ModId, StringComparer.Ordinal)
            .ThenBy(item => item.PublishedFileId, StringComparer.Ordinal).ToArray();

    public bool TryPublish(out string? error)
    {
        error = null;
        if (_pending == null || !_pending.IsCompleted)
            return false;

        var completed = _pending;
        _pending = null;
        try
        {
            Tags = SteamWorkshopTagService.MapTagsToMods(_installations, completed.GetAwaiter().GetResult());
            return true;
        }
        catch (Exception ex)
        {
            _failed = true;
            error = ex.Message;
            return false;
        }
    }
}
