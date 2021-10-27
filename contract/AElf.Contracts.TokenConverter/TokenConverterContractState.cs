using AElf.Standards.ACS1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS10;
using AElf.Types;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal DividendPoolContractContainer.DividendPoolContractReferenceState DividendPoolContract { get; set; }

        public StringState BaseTokenSymbol { get; set; }
        public StringState FeeRate { get; set; }
        public MappedState<string, Connector> Connectors { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public MappedState<string, long> DepositBalance { get; set; }
        public SingletonState<AuthorityInfo> ConnectorController { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}