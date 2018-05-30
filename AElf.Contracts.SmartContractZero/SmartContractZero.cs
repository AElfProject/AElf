using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Contracts
{
    public class SmartContractZero : CSharpSmartContract, ISmartContractZero
    {
        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            Type type = typeof(SmartContractZero);

            // method info
            var member = type.GetMethod(tx.MethodName);

            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            // invoke
            await (Task) member.Invoke(this, parameters);
        }

        /// <inheritdoc/>
        //public async Task RegisterSmartContract(SmartContractRegistration reg)
        //{
        //    var hash = reg.ContractHash;
        //    await SmartContractMap.SetValueAsync(hash, reg.Serialize());
        //}

        public async Task DeploySmartContract(SmartContractRegistration registration)
        {
            // TODO: Check permission
            var tx = Api.GetTransaction();

            // calculate new account address
            var account = Path.CalculateAccountAddress(tx.From, tx.IncrementId);

            // set storage
            await Api.DeployContractAsync(account, registration);

            // TODO: Log New Account Address to TransactionResult
            Api.LogToResult(account.Value.ToByteArray());
        }

        public Hash GetHash()
        {
            return Hash.Zero;
        }
    }
}
