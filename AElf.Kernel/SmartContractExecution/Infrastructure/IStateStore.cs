namespace AElf.Kernel.Storages
{
    public interface IStateStore<T> : IKeyValueStore<T>
    {
    }

    public interface IStateStore : IKeyValueStore
    {
        
    }
}