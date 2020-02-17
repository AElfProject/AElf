using Acs1;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractState : ContractState
    {
        public StringState BaseTokenSymbol { get; set; }
        public StringState FeeRate { get; set; }
        public MappedState<string, Connector> Connectors { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        public SingletonState<Address> FeeReceiverAddress { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public MappedState<string, long> DepositBalance { get; set; }
        public SingletonState<Address> ControllerForManageConnector { get; set; }

        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}