using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis;

public partial class GenesisContractAuthTest
{
    [Fact]
    public async Task DeploySmartContract_Deterministic_Test()
    {
        StartSideChain();
        var sideChainId = SideChainTester.GetChainAsync().Result.Id;

        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value);

        var contractAddress = await DeployContractOnMainChain();
        
        var output = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetContractInfo), contractAddress);
        var contractInfo = ContractInfo.Parser.ParseFrom(output);
        
        contractInfo.SerialNumber.ShouldBe(0);
        contractInfo.DeployingAddress.ShouldBe(CreatorAddress);
        contractInfo.Salt.ShouldBe(HashHelper.ComputeFrom("test"));

        var contractDeploymentInput2 = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = sideChainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = 1,
                Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, sideChainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"), 1)
            }
        };
        var contractAddress2 = await DeployAsync(SideChainTester, SideParliamentAddress, SideBasicContractZeroAddress,
            contractDeploymentInput2);
        contractAddress2.ShouldNotBeNull();

        contractAddress.ShouldBe(contractAddress2);

        code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value);
        var result = await DeployWithResultAsync(SideChainTester, SideParliamentAddress, SideBasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    ChainId = sideChainId,
                    CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    Version = 1,
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, sideChainId,
                        HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"), 1)
                }
            });
        result.Error.ShouldContain("Contract address exists");
    }

    [Fact]
    public async Task DeploySmartContract_Deterministic_ContractOperation_Test()
    {
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value);
        var mainChainId = Tester.GetChainAsync().Result.Id;

        var result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation()
            });
        result.Error.ShouldContain("Invalid input deploying address.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = new Address()
                }
            });
        result.Error.ShouldContain("Invalid input deploying address.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress
                }
            });
        result.Error.ShouldContain("Invalid input salt.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = new Hash()
                }
            });
        result.Error.ShouldContain("Invalid input salt.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test")
                }
            });
        result.Error.ShouldContain("Invalid input code hash.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = new Hash()
                }
            });
        result.Error.ShouldContain("Invalid input code hash.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom("code")
                }
            });
        result.Error.ShouldContain("Invalid input signature.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom("code"),
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                        HashHelper.ComputeFrom("code"), CreatorAddress, HashHelper.ComputeFrom("test"), 1)
                }
            });
        result.Error.ShouldContain("Wrong input version.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom("code"),
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                        HashHelper.ComputeFrom("code"), CreatorAddress, HashHelper.ComputeFrom("test"), 1),
                    Version = 2
                }
            });
        result.Error.ShouldContain("Wrong input version.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom("code"),
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                        HashHelper.ComputeFrom("code"), CreatorAddress, HashHelper.ComputeFrom("test"), 0),
                    Version = 1,
                    ChainId = 1
                }
            });
        result.Error.ShouldContain("Wrong input chain id.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom("code"),
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                        HashHelper.ComputeFrom("code"), CreatorAddress, HashHelper.ComputeFrom("test"), 0),
                    Version = 1,
                    ChainId = mainChainId
                }
            });
        result.Error.ShouldContain("Wrong input code hash.");

        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code,
                ContractOperation = new ContractOperation
                {
                    DeployingAddress = CreatorAddress,
                    Salt = HashHelper.ComputeFrom("test"),
                    CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                    Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                        HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"), 0),
                    Version = 1,
                    ChainId = mainChainId
                }
            });
        result.Error.ShouldContain("Wrong signature.");

        // compatible with old version
        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = code
            });
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task DeploySmartContract_Deterministic_Delegate_Test()
    {
        var mainChainId = Tester.GetChainAsync().Result.Id;
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value);

        var contractDeploymentInput = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = mainChainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = 1,
                Signature = GenerateContractSignature(SignerKeyPair.PrivateKey, mainChainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"), 1)
            }
        };

        var result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            contractDeploymentInput);
        result.Error.ShouldContain("Wrong signature.");

        var tester = Tester.CreateNewContractTester(CreatorKeyPair);
        var output = await tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetDelegateSignatureAddress), CreatorAddress);
        output.Length.ShouldBe(0);

        await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.SetDelegateSignatureAddress), SignerAddress);
        output = await tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetDelegateSignatureAddress), CreatorAddress);
        var delegateAddress = Address.Parser.ParseFrom(output.ToByteArray());
        delegateAddress.ShouldBe(SignerAddress);

        await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.RemoveDelegateSignatureAddress), new Empty());
        output = await tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetDelegateSignatureAddress), CreatorAddress);
        output.Length.ShouldBe(0);

        await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.SetDelegateSignatureAddress), SignerAddress);
        result = await DeployWithResultAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            contractDeploymentInput);
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task UpdateSmartContract_Deterministic_Test()
    {
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value);
        var chainId = Tester.GetChainAsync().Result.Id;

        var contractAddress = await DeployContractOnMainChain();

        var output = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetContractInfo), contractAddress);
        var contractInfo = ContractInfo.Parser.ParseFrom(output);

        var result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = chainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = contractInfo.Version + 1,
                Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, chainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"),
                    contractInfo.Version + 1)
            }
        });
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }
    
    [Fact]
    public async Task UpdateSmartContract_Deterministic_Compatible_Test()
    {
        var contractAddress = await DeployAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value)
            });

        var result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value)
        });
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }
    
    [Fact]
    public async Task UpdateSmartContract_Deterministic_Compatible_OldDeployNewUpdate_Test()
    {
        var contractAddress = await DeployAsync(Tester, ParliamentAddress, BasicContractZeroAddress,
            new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value)
            });

        var chainId = Tester.GetChainAsync().Result.Id;
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value);
        
        var output = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetContractInfo), contractAddress);
        var contractInfo = ContractInfo.Parser.ParseFrom(output);

        var result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = chainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = contractInfo.Version + 1,
                Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, chainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"),
                    contractInfo.Version + 1)
            }
        });
        result.Error.ShouldContain("Not compatible.");
    }
    
    [Fact]
    public async Task UpdateSmartContract_Deterministic_Compatible_NewDeployOldUpdate_Test()
    {
        var contractAddress = await DeployContractOnMainChain();

        var result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value)
        });
        result.Error.ShouldContain("Not compatible.");
    }
    
    [Fact]
    public async Task UpdateSmartContract_Deterministic_ContractOperation_Test()
    {
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicSecurity")).Value);
        var chainId = Tester.GetChainAsync().Result.Id;

        var contractAddress = await DeployContractOnMainChain();

        var output = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.GetContractInfo), contractAddress);
        var contractInfo = ContractInfo.Parser.ParseFrom(output);

        var result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = chainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("fail"),
                Version = contractInfo.Version + 1,
                Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, chainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("fail"),
                    contractInfo.Version + 1)
            }
        });
        result.Error.ShouldContain("No permission.");
        
        result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = chainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = SignerAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = contractInfo.Version + 1,
                Signature = GenerateContractSignature(SignerKeyPair.PrivateKey, chainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), SignerAddress, HashHelper.ComputeFrom("test"),
                    contractInfo.Version + 1)
            }
        });
        result.Error.ShouldContain("No permission.");
        
        result = await UpdateAsync(Tester, ParliamentAddress, BasicContractZeroAddress, new ContractUpdateInput
        {
            Address = contractAddress,
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = chainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = SignerAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = 0,
                Signature = GenerateContractSignature(SignerKeyPair.PrivateKey, chainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), SignerAddress, HashHelper.ComputeFrom("test"),
                    0)
            }
        });
        result.Error.ShouldContain("Wrong input version.");
    }
    
    private async Task<Address> DeployContractOnMainChain()
    {
        var mainChainId = Tester.GetChainAsync().Result.Id;
        var code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TestContract.BasicFunction")).Value);

        var contractDeploymentInput = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = code,
            ContractOperation = new ContractOperation
            {
                ChainId = mainChainId,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                DeployingAddress = CreatorAddress,
                Salt = HashHelper.ComputeFrom("test"),
                Version = 1,
                Signature = GenerateContractSignature(CreatorKeyPair.PrivateKey, mainChainId,
                    HashHelper.ComputeFrom(code.ToByteArray()), CreatorAddress, HashHelper.ComputeFrom("test"), 1)
            }
        };

        var contractAddress =
            await DeployAsync(Tester, ParliamentAddress, BasicContractZeroAddress, contractDeploymentInput);
        contractAddress.ShouldNotBeNull();

        return contractAddress;
    }

    private ByteString GenerateContractSignature(byte[] privateKey, int chainId, Hash codeHash,
        Address address, Hash salt, int version)
    {
        var data = new ContractOperation
        {
            ChainId = chainId,
            CodeHash = codeHash,
            DeployingAddress = address,
            Salt = salt,
            Version = version
        };
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}