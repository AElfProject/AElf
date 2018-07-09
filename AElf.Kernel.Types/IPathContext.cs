// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <summary>
    /// In our design, a Path (or PathContext to avoid confusion
    /// with System.IO.Path) is used for locating a resource,
    /// while a Pointer is exactily the key to get the resource's
    /// value of one specific height (and mined by one specific
    /// block producer) from database.
    /// A resource can be anything persistented in database like
    /// the balance of an account, the timeslot of a block producer,
    /// etc.
    /// </summary>
    public interface IPathContext
    {
        /*
         * To compose a PathContext, we need:
         * Chain Id - (Previous) Block Hash - Block Producer Address -
         * Account Address - Data Provider (can be multiple levels)
         */
        PathContext SetChainId(Hash chainId);
        PathContext SetBlockHash(Hash blockHash);
        PathContext SetBlockProducerAddress(Hash blockProducerAddress);
        PathContext SetAccountAddress(Hash accountAddress);
        PathContext SetDataProvider(Hash dataProvider);
        
        Hash GetPointerHash();
        Hash GetPathHash();
    }
}