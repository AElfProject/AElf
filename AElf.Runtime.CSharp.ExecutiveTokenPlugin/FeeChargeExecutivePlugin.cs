using System;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Token;
using AElf.Kernel.Types.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    public class FeeChargeExecutivePlugin : IExecutivePlugin, ITransientDependency
    {
        //TODO: Add FeeChargeExecutivePlugin->AfterApply test case [Case]
        public void AfterApply(IHostSmartContractBridgeContext context,
            Func<string, IMessage, IMessage> executeReadOnlyHandler)
        {
            /*if (!(smartContract is IFeeChargedContract) || context.TransactionContext.CallDepth > 0)
            {
                return;
            }*/

            var fee = (Int64Value) executeReadOnlyHandler(nameof(IFeeChargedContract.GetMethodFee),
                new StringValue() {Value = context.TransactionContext.Transaction.MethodName});


            context.TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = context.TransactionContext.Transaction.From,
                To = context.GetContractAddressByName(
                    TokenSmartContractAddressNameProvider.Name),
                MethodName =
                    "ChargeTransactionFees", // TODO: Use `nameof`, maybe need to add a ref or an interface for token contract.
                Params = fee.ToByteString()
            });
        }
    }
}