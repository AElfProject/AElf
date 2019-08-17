using System;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContractState
    {
        public Boolean Initialized { get; set; }
        public Int64State MinBet { get; set; }
        public Int64State MaxBet { get; set; }
        
        public MappedState<string, long> SupportTokens { get; set; }
        public MappedState<Address, long> RewardHistory { get; set; }
        public MappedState<Address, long> BetHistory { get; set; }
    }
}