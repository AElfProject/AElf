// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <summary>
    /// In our design, a ResourcePath is used for locating a resource,
    /// while a Pointer is exactly the key to get the resource's
    /// value of one specific height (and mined by one specific
    /// block producer) from database.
    /// 
    /// A resource can be anything persisted in database like the
    /// balance of an account, the timeslot of a block producer,
    /// etc.
    /// </summary>
    public interface IResourcePath
    {
        /*
         * To compose a PathContext, we need:
         * Chain Id - (Previous) Block Hash - Block Producer Address -
         * Account Address - Data Provider (can be multiple levels)
         */
        ResourcePath SetChainId(Hash chainId);
        ResourcePath SetBlockHash(Hash blockHash);
        ResourcePath SetBlockProducerAddress(Hash blockProducerAddress);
        ResourcePath SetAccountAddress(Hash accountAddress);
        ResourcePath SetDataProvider(Hash dataProvider);
        
        Hash GetPointerHash();
        Hash GetPathHash();
    }
}