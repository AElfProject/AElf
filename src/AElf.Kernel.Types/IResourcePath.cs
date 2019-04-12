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
        /// <summary>
        /// To identity one specific block's state.
        /// Chain Id - Round Number - Block Producer Account
        /// </summary>
        Hash StateHash { get; }

        /// <summary>
        /// Data Provider - Data Key
        /// </summary>
        Hash ResourcePathHash { get; }
        
        /// <summary>
        /// StateHash - DataHash
        /// </summary>
        Hash ResourcePointerHash { get; }

        ResourcePath SetChainId(Hash chainId);
        ResourcePath SetRoundNumber(ulong roundNumber);
        ResourcePath SetBlockProducerAddress(Hash blockProducerAddress);

        ResourcePath SetAccountAddress(Hash contractAddress);
        ResourcePath SetDataProvider(Hash dataProvider);
        ResourcePath SetDataKey(Hash keyHash);

        /// <summary>
        /// Remove State.
        /// </summary>
        /// <returns></returns>
        ResourcePath RemoveState();

        /// <summary>
        /// Remove Path.
        /// </summary>
        /// <returns></returns>
        ResourcePath RemovePath();
    }
}