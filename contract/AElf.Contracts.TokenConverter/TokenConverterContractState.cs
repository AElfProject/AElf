using Acs1;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractState : ContractState
    {
        public StringState BaseTokenSymbol { get; set; }
        public StringState FeeRate { get; set; }
        public Int32State ConnectorCount { get; set; }
        public MappedState<int, string> ConnectorSymbols { get; set; }
        public MappedState<string, Connector> Connectors { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
        public SingletonState<Address> FeeReceiverAddress { get; set; }
        public SingletonState<Address> ManagerAddress { get; set; }
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }


    }
}