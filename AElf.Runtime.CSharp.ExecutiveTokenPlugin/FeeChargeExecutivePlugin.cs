using System;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Token;
using AElf.Kernel.Types.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    public class FeeChargeExecutivePlugin : IExecutivePlugin, ITransientDependency
    {
        public void AfterApply(ISmartContract smartContract, IHostSmartContractBridgeContext context,
            Func<string, object[], object> executeReadOnlyHandler)
        {
            if (!(smartContract is IFeeChargedContract) || context.TransactionContext.CallDepth > 0)
            {
                return;
            }

            var fee = (ulong) executeReadOnlyHandler(nameof(IFeeChargedContract.GetMethodFee),
                
                new object[] {context.TransactionContext.Transaction.MethodName});

            context.TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = context.TransactionContext.Transaction.From,
                To = context.GetContractAddressByName(
                    TokenSmartContractAddressNameProvider.Name),
                MethodName = nameof(ITokenContract.ChargeTransactionFees),
                Params = ByteString.CopyFrom(
                    ParamsPacker.Pack(fee))
            });
        }
    }
}