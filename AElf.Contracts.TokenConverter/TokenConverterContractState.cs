using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractState : ContractState
    {
        public Int32State ConnectorCount { get; set; }
        public StringState BaseTokenSymbol { get; set; }
        public Int64State FeeRateNumerator { get; set; }
        public Int64State FeeRateDenominator { get; set; }
        public Int32State MaxWeight { get; set; }
        public MappedState<int, string> ConnectorSymbols { get; set; }
        public MappedState<string, Connector> Connectors { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        public SingletonState<Address> FeeReceiverAddress { get; set; }
        public SingletonState<Address> Manager { get; set; }
    }
}