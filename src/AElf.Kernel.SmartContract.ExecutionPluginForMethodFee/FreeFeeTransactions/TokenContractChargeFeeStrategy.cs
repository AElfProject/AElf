using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    public class TokenContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TokenContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

        public string MethodName => string.Empty;

        public bool IsFree(Transaction transaction)
        {
            // Stop charging fee from system txs and plugin txs.
            return new List<string>
            {
                // System tx
                nameof(TokenContractImplContainer.TokenContractImplStub.ClaimTransactionFees),
                nameof(TokenContractImplContainer.TokenContractImplStub.DonateResourceToken),

                // Pre-plugin tx
                nameof(TokenContractImplContainer.TokenContractImplStub.ChargeTransactionFees),
                nameof(TokenContractImplContainer.TokenContractImplStub.CheckThreshold),
                nameof(TokenContractImplContainer.TokenContractImplStub.CheckResourceToken),

                // Post-plugin tx
                nameof(TokenContractImplContainer.TokenContractImplStub.ChargeResourceToken),
            }.Contains(transaction.MethodName);
        }
    }
}