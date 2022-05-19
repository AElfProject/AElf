using System.Collections.Generic;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public class WhitelistContractState : ContractState
    {
        public MappedState<Hash, BytesValue> ExtraInfoMap { get; set; }

        public MappedState<Address, WhitelistIdList> WhitelistIdMap { get; set; }
        
        public MappedState<Hash, WhitelistInfo> WhitelistInfoMap { get; set; }
        
        /// <summary>
        /// whitelist_id -> manager address list
        /// </summary>
        public MappedState<Hash, AddressList> ManagerListMap { get; set; }

        public MappedState<Hash, SubscribeWhitelistInfo> SubscribeWhitelistInfoMap { get; set; }

        public MappedState<Hash, ConsumedList> ConsumedListMap { get; set; }

    }
}