using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Kernel.Tests
{
    public class TestContractZero : CSharpSmartContract, ISmartContractZero
    {
        public override async Task InvokeAsync()
        {
            await Task.CompletedTask;
        }

        [SmartContractFunction("${this}.DeploySmartContract", new string[]{}, new string[]{})]
        public async Task<Hash> DeploySmartContract(SmartContractRegistration registration)
        {
            var tx = Api.GetTransaction();

            var code = registration.ContractBytes.ToByteArray();
            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            }

            if (assembly == null)
            {
                Console.WriteLine("Invalid code");
                throw new InvalidDataException("Invalid binary code.");
            }
            else
            {
                Console.WriteLine(assembly.GetType().Name);
            }
            
            
            // calculate new account address
            var account = Path.CalculateAccountAddress(tx.From, tx.IncrementId);
            
            await Api.DeployContractAsync(account, registration);
            return account;
        }
    }
}
