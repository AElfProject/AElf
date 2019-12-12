using Acs1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContractState
    {
        internal MappedState<string, MethodFees> MethodFees { get; set; }
        public Int64State TransactionFeeUnitPrice { get; set; }
        public MappedState<FeeTypeEnum, CalculateFeeCoefficientsOfType> CalculateCoefficientOfContract { get; set; }
        public CalculateFeeCoefficientsOfType CalculateCoefficientOfSender { get; set; }
        
    }
}