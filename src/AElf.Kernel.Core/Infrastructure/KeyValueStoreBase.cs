using System.IO;
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
        return ToByteArrayEfficiently(value);
    }
    
    public static byte[] ToByteArrayEfficiently( IMessage message)
    {
        ProtoPreconditions.CheckNotNull<IMessage>(message, nameof(message));

        // 使用MemoryStream来动态分配内存
        using (var memoryStream = new MemoryStream())
        {
            // 创建一个CodedOutputStream来写入MemoryStream
            using (var codedOutput = new CodedOutputStream(memoryStream))
            {
                // 写入消息
                message.WriteTo(codedOutput);
                // 刷新CodedOutputStream以确保所有数据都已写入MemoryStream
                codedOutput.Flush();
            }

            // 将MemoryStream的内容转换为字节数组
            return memoryStream.ToArray();
        }
    }

    private T Deserialize(byte[] result)
    {
        return _messageParser.ParseFrom(result);
    }
}