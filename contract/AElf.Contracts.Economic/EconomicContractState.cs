using System;
using Acs0;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Economic
{
    public class EconomicContractState : ContractState
    {
        public SingletonState<bool> Initialized { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
        internal ACS0Container.ACS0ReferenceState ACS0Contract { get; set; }
    }
}