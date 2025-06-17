using System.Collections.Concurrent;

namespace ledger11.auth.Services;

public interface IAuthCodeStore
{
    void Store(string code, AuthRequestInfo info);
    bool TryRetrieve(string code, out AuthRequestInfo? info);
    void Remove(string code);
}

public class InMemoryAuthCodeStore : IAuthCodeStore
{
    private readonly ConcurrentDictionary<string, AuthRequestInfo> _store = new();

    public void Store(string code, AuthRequestInfo info)
    {
        _store[code] = info;
    }

    public bool TryRetrieve(string code, out AuthRequestInfo? info)
    {
        return _store.TryRemove(code, out info);
    }

    public void Remove(string code)
    {
        _store.TryRemove(code, out _);
    }
}
