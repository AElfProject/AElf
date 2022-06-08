using System.Linq;
using AElf.Database;

namespace AElf.Kernel.Infrastructure;

public interface IStoreKeyPrefixProvider<T>
    where T : IMessage<T>, new()
{
    string GetStoreKeyPrefix();
}

public class StoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
    where T : IMessage<T>, new()
{
    private static readonly string TypeName = typeof(T).Name;

    public string GetStoreKeyPrefix()
    {
        return TypeName;
    }
}

public class FastStoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
    where T : IMessage<T>, new()
{
    private readonly string _prefix;

    public FastStoreKeyPrefixProvider(string prefix)
    {
        _prefix = prefix;
    }

    public string GetStoreKeyPrefix()
    {
        return _prefix;
    }
}

public abstract class KeyValueStoreBase<TKeyValueDbContext, T> : IKeyValueStore<T>
    where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    where T : class, IMessage<T>, new()
{
    private readonly IKeyValueCollection _collection;

    private readonly MessageParser<T> _messageParser;

    public KeyValueStoreBase(TKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider)
    {
        _collection = keyValueDbContext.Collection(prefixProvider.GetStoreKeyPrefix());

        _messageParser = new MessageParser<T>(() => new T());
    }

    public async Task SetAsync(string key, T value)
    {
        await _collection.SetAsync(key, Serialize(value));
    }

    public async Task SetAllAsync(Dictionary<string, T> pipelineSet)
    {
        await _collection.SetAllAsync(
            pipelineSet.ToDictionary(k => k.Key, v => Serialize(v.Value)));
    }

    public virtual async Task<T> GetAsync(string key)
    {
        var result = await _collection.GetAsync(key);

        return result == null ? default : Deserialize(result);
    }

    public virtual async Task RemoveAsync(string key)
    {
        await _collection.RemoveAsync(key);
    }

    public async Task<bool> IsExistsAsync(string key)
    {
        return await _collection.IsExistsAsync(key);
    }

    public virtual async Task<List<T>> GetAllAsync(List<string> keys)
    {
        var result = await _collection.GetAllAsync(keys);

        return result == null || result.Count == 0
            ? default
            : result.Select(r => r == null ? default : Deserialize(r)).ToList();
    }

    public virtual async Task RemoveAllAsync(List<string> keys)
    {
        await _collection.RemoveAllAsync(keys);
    }

    private static byte[] Serialize(T value)
    {
        return value?.ToByteArray();
    }

    private T Deserialize(byte[] result)
    {
        return _messageParser.ParseFrom(result);
    }
}