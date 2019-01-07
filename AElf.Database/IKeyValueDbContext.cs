using System.Dynamic;

namespace AElf.Database
{
    public interface IKeyValueDbContext
    {
        IKeyValueDatabase Database { get; }

        IKeyValueCollection<TValue> Collection<TValue>(string name);
    }
}