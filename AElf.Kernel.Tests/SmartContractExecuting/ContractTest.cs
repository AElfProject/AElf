using System;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Xunit;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    public class ContractTest
    {

        [Fact]
        public void RegisterContract()
        {
            Type smartContractZero = typeof(Class1);
            
            var registerTx = new Transaction
            {
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                To = Hash.Zero,
                From = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = 
                        {
                            new Param
                            {
                                RegisterVal = new SmartContractRegistration
                                {
                                    Category = 0,
                                    ContractBytes = ByteString.CopyFromUtf8(smartContractZero.FullName),
                                    ContractHash = Hash.Zero
                                }
                            }
                        }
                    }.ToByteArray()
                )
            };
        }
        
        
        public void DeployContract()
        {
            var deployTx = new Transaction
            {
                IncrementId = 1,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                From = Hash.Zero,
                To = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new Parameters
                        {
                            Params = { 
                                new Param
                                {
                                    HashVal = Hash.Zero
                                },
                                new Param
                                {
                                    DeploymentVal = new SmartContractDeployment
                                    {
                                        ContractHash = Hash.Zero
                                    }
                                }}
                        }
                        .ToByteArray()
                )
            };
        }
    }
}