using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public class WhitelistContractState : ContractState
    {
        public MappedState<Hash, BytesValue> ExtraInfoMap { get; set; }

        public MappedState<Hash, WhitelistInfo> WhitelistInfoMap { get; set; }

        public MappedState<Hash, SubscribeWhitelistInfo> SubscribeWhitelistInfoMap { get; set; }

        public MappedState<Hash, ConsumedList> ConsumedListMap { get; set; }

        public MappedState<Hash, WhitelistInfo> CloneWhitelistInfoMap { get; set; }
    }
}