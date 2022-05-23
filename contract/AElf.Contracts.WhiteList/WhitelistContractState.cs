using System.Collections.Generic;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public class WhitelistContractState : ContractState
    {

        /// <summary>
        ///TagInfo id -> TagInfo(name,info)
        /// </summary>
        public MappedState<Hash, TagInfo> TagInfoMap { get; set; }

        /// <summary>
        ///Manager -> Project Id -> whitelistId -> TagInfo Id List
        /// </summary>
        public MappedState<Address, Hash, Hash, HashList> ManagerTagInfoMap { get; set; }
        
        /// <summary>
        ///WhitelistId -> TagInfoId -> AddressList
        /// </summary>
        public MappedState<Hash, Hash, AddressList> TagInfoIdAddressListMap { get; set; }

        /// <summary>
        ///WhitelistId -> Address -> TagInfo Id
        /// </summary>
        public MappedState<Hash, Address, Hash> AddressTagInfoIdMap { get; set; }

        /// <summary>
        /// Manager address -> Whitelist id list.
        /// </summary>
        public MappedState<Address, WhitelistIdList> WhitelistIdMap { get; set; }
        
        /// <summary>
        /// Whitelist id -> Whitelist Info.
        /// </summary>
        public MappedState<Hash, WhitelistInfo> WhitelistInfoMap { get; set; }

        /// <summary>
        ///Project id -> Whitelist id list.
        /// </summary>
        /// <returns></returns>
        public MappedState<Hash, WhitelistIdList> WhitelistProjectMap { get; set; }

        /// <summary>
        /// whitelist_id -> manager address list
        /// </summary>
        public MappedState<Hash, AddressList> ManagerListMap { get; set; }

        public MappedState<Hash, SubscribeWhitelistInfo> SubscribeWhitelistInfoMap { get; set; }

        public MappedState<Hash, ConsumedList> ConsumedListMap { get; set; }

    }
}