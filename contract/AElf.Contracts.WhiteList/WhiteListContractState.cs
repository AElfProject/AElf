using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.WhiteList
{
    public class WhiteListContractState : ContractState
    {
        public MappedState<Hash, ExtraInfo> ExtraInfoMap { get; set; }
        
        public MappedState<Hash, WhiteListInfo> WhiteListInfoMap { get; set; }

        public MappedState<Hash, SubscribeWhiteListInfo> SubscribeWhiteListInfoMap { get; set; }
        
        public MappedState<Hash,ConsumedList> ConsumedListMap { get; set; }
        
        public MappedState<Hash, WhiteListInfo> CloneWhiteListInfoMap { get; set; }

    }
}