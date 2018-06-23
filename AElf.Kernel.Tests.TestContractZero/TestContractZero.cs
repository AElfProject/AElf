using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Kernel.Tests
{
    public class TestContractZero : CSharpSmartContract, ISmartContractZero
    {
        [SmartContractFieldData("${this}._lock", DataAccessMode.ReadWriteAccountSharing)]
        private object _lock;
        public override async Task InvokeAsync()
        {
            await Task.CompletedTask;
        }

        [SmartContractFunction("${this}.DeploySmartContract", new string[]{}, new string[]{"${this}._lock"})]
        public async Task<Hash> DeploySmartContract(int category, byte[] contract)
        {
            Console.WriteLine("categort " + category);
            SmartContractRegistration registration = new SmartContractRegistration
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(contract),
                ContractHash = contract.CalculateHash() // maybe no usage  
            };
            
            var tx = Api.GetTransaction();
            
            // calculate new account address
            var account = Path.CalculateAccountAddress(tx.From, tx.IncrementId).ToAccount();
            await Api.DeployContractAsync(account, registration);
            Console.WriteLine("Deployment success, contract address: {0}", account.Value.ToBase64());
            return account;
        }

        public void Print(string name)
        {
            Console.WriteLine("Hello, " + name);
        }
        
    }
}
