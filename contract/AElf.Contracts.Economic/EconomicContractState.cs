﻿using Acs0;
using Acs1;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Economic
{
    public class EconomicContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal ACS0Container.ACS0ReferenceState ZeroContract { get; set; }
        internal MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}