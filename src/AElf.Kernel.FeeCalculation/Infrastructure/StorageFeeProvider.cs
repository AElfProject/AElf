﻿using AElf.Contracts.MultiToken;
using Volo.Abp.DependencyInjection;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class StorageFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        public StorageFeeProvider(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsProvider, calculateFunctionProvider, (int) FeeTypeEnum.Storage)
        {

        }

        public string TokenName => "STORAGE";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}