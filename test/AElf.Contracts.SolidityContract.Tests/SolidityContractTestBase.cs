using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Solang;
using Solang.Extensions;
using Volo.Abp.Threading;
using Transaction = AElf.Types.Transaction;

namespace AElf.Contracts.SolidityContract;

public class SolidityContractTestBase : ContractTestBase<SolidityContractTestAElfModule>
{
    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }

    internal readonly IBlockchainService BlockchainService;
    internal readonly ISmartContractAddressService SmartContractAddressService;
    internal readonly ITestTransactionExecutor TestTransactionExecutor;
    internal readonly IRefBlockInfoProvider RefBlockInfoProvider;


    public SolidityContractTestBase()
    {
        SmartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        BlockchainService = GetRequiredService<IBlockchainService>();
        TestTransactionExecutor = GetRequiredService<ITestTransactionExecutor>();
        RefBlockInfoProvider = GetRequiredService<IRefBlockInfoProvider>();
        InitializeContracts();
    }

    protected void InitializeContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);
        //deploy token contract
        AsyncHelper.RunSync(() => BasicContractZeroStub
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                    Name = TokenSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateTokenInitializationCallList()
                }));
    }
    
    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateTokenInitializationCallList()
    {
        var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = SolidityContractTestConstants.NativeTokenSymbol,
            Decimals = 8,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = SolidityContractTestConstants.NativeTokenTotalSupply,
            Issuer = DefaultSender,
            Owner = DefaultSender
        });
        tokenContractCallList.Add(nameof(TokenContract.SetPrimaryTokenSymbol),
            new SetPrimaryTokenSymbolInput { Symbol = "ELF" });
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = SolidityContractTestConstants.NativeTokenSymbol,
            Amount = (long)(SolidityContractTestConstants.NativeTokenTotalSupply * 0.12),
            To = Address.FromPublicKey(DefaultSenderKeyPair.PublicKey),
            Memo = "Issue token to default user for vote."
        });

        return tokenContractCallList;
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal async Task<IExecutionResult<Address>> DeployWebAssemblyContractAsync(byte[] codeBytes,
        ByteString constructorInput = null)
    {
        var executionResult = await BasicContractZeroStub.DeploySmartContract.SendAsync(new ContractDeploymentInput
        {
            Category = KernelConstants.SolidityRunnerCategory,
            Code = ByteString.CopyFrom(codeBytes),
            Parameter = constructorInput ?? ByteString.Empty
        });

        return executionResult;
    }

    internal async Task<Transaction> GetTransactionAsync(ECKeyPair keyPair, Address to, string methodName,
        ByteString parameter = null)
    {
        var refBlockInfo = RefBlockInfoProvider.GetRefBlockInfo();
        var transaction =
            await GetTransactionWithoutSignatureAsync(Address.FromPublicKey(keyPair.PublicKey), to, methodName, parameter);
        transaction.RefBlockNumber = refBlockInfo.Height;
        transaction.RefBlockPrefix = refBlockInfo.Prefix;

        var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);
        return transaction;
    }

    private async Task<Transaction> GetTransactionWithoutSignatureAsync(Address from, Address to, string methodName,
        ByteString parameter)
    {
        var registration = await BasicContractZeroStub.GetSmartContractRegistrationByAddress.CallAsync(to);
        var solangAbi =
            JsonSerializer.Deserialize<SolangABI>(new Compiler().BuildWasm(registration.Code.ToByteArray()).Contracts.First()
                .Abi);
        var selector = methodName == "deploy" ? solangAbi.GetConstructor() : solangAbi.GetSelector(methodName);
        var transaction = new Transaction
        {
            From = from,
            To = to,
            MethodName = selector,
            Params = parameter ?? ByteString.Empty
        };

        return transaction;
    }

    internal ByteString Index(int index)
    {
        return ByteString.CopyFrom(new ABIEncode().GetABIEncoded(new ABIValue("uint256", index)));
    }
}