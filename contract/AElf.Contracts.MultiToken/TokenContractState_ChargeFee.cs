using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContractState
    {
        internal MappedState<string, MethodFees> MethodFees { get; set; }

        public SingletonState<Timestamp> LastPayRentTime { get; set; }

        public SingletonState<Address> SideChainCreator { get; set; }

        public MappedState<FeeTypeEnum, CalculateFeeCoefficientsOfType> CalculateCoefficientOfContract { get; set; }
        public SingletonState<CalculateFeeCoefficientsOfType> CalculateCoefficientOfSender { get; set; }
        public SingletonState<SymbolListToPayTXSizeFee> SymbolListToPayTxSizeFee { get; set; }

        /// <summary>
        /// Symbol -> Amount (TBD)
        /// (CPU: core)
        /// (RAM: GiB)
        /// (DISK: GiB)
        /// (NET: MB)
        /// </summary>
        public MappedState<string, int> ResourceAmount { get; set; }

        /// <summary>
        /// Symbol -> Amount (Tokens per minute)
        /// </summary>
        public MappedState<string, long> Rental { get; set; }

        /// <summary>
        /// Symbol -> Amount
        /// </summary>
        public MappedState<string, long> OwningRental { get; set; }
    }
}