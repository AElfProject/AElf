using System;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
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
        public static bool IsAcs1(ServerServiceDefinition definition)
        {
            var binder = new DefaultServiceBinder();
            definition.BindService(binder);
            return binder.GetDescriptors().Any(service => service.File.GetIndentity() == "acs1");
        }

        public void PostMain(IHostSmartContractBridgeContext context, ServerServiceDefinition definition)
        {
            if (!IsAcs1(definition))
            {
                return;
            }

            var stub = new FeeChargedContractContainer.FeeChargedContractStub()
            {
                __factory = new MethodStubFactory(context)
            };

            var fee = stub.GetMethodFee.CallAsync(new GetMethodFeeInput()
            {
                Method = context.TransactionContext.Transaction.MethodName
            }).Result.Fee;

            context.TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = context.TransactionContext.Transaction.From,
                To = context.GetContractAddressByName(
                    TokenSmartContractAddressNameProvider.Name),
                MethodName = nameof(TokenContractContainer.TokenContractStub.ChargeTransactionFees),
                Params = new ChargeTransactionFeesInput()
                {
                    Amount = fee,
                    Symbol = "ELF" // Temporary hardcode symbol
                }.ToByteString()
            });
        }
    }
}