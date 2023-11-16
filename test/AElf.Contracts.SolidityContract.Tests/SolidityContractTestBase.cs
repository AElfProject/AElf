using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.SolidityContract.Extensions;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Runtime.WebAssembly;
using AElf.SolidityContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util.ByteArrayConvertors;
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
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

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
        var tokenContractAddress = (AsyncHelper.RunSync(() => BasicContractZeroStub
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                    Name = TokenSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateTokenInitializationCallList()
                }))).Output;
        TokenContractStub =
            GetTester<TokenContractContainer.TokenContractStub>(tokenContractAddress, DefaultSenderKeyPair);
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

    internal async Task<IExecutionResult<Address>> DeployWasmContractAsync(byte[] codeBytes,
        ByteString constructorInput = null)
    {
        var compiledOutput = new Compiler().BuildWasm(codeBytes);
        var abi = compiledOutput.Contracts.First().Abi;
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(abi);
        var code = solangAbi.Source.Wasm.HexToByteArray();
        var wasmCode = new WasmContractCode
        {
            Code = ByteString.CopyFrom(code),
            Abi = abi,
            CodeHash = Hash.LoadFromHex(solangAbi.Source.Hash)
        };
        return await DeployWasmContractAsync(wasmCode, constructorInput);
    }

    internal async Task<IExecutionResult<Address>> DeployWasmContractAsync(WasmContractCode wasmCode,
        ByteString constructorInput = null)
    {
        var executionResult = await BasicContractZeroStub.DeploySoliditySmartContract.SendAsync(
            new DeploySoliditySmartContractInput
            {
                Category = KernelConstants.WasmRunnerCategory,
                Code = wasmCode.ToByteString(),
                Parameter = constructorInput ?? ByteString.Empty
            });

        return executionResult;
    }

    internal async Task<Transaction> GetTransactionAsync(ECKeyPair keyPair, Address to, string methodName,
        ByteString parameter = null, long value = 0)
    {
        var refBlockInfo = RefBlockInfoProvider.GetRefBlockInfo();
        var transaction =
            await GetTransactionWithoutSignatureAsync(Address.FromPublicKey(keyPair.PublicKey), to, methodName,
                parameter, value);
        transaction.RefBlockNumber = refBlockInfo.Height;
        transaction.RefBlockPrefix = refBlockInfo.Prefix;

        var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);
        return transaction;
    }
    
    internal async Task<Transaction> GetTransactionAsync(ECKeyPair keyPair, Address to, string methodName,
        string parameter, long value = 0)
    {
        var refBlockInfo = RefBlockInfoProvider.GetRefBlockInfo();
        var transaction =
            await GetTransactionWithoutSignatureAsync(Address.FromPublicKey(keyPair.PublicKey), to, methodName,
                ByteString.CopyFrom(new HexToByteArrayConvertor().ConvertToByteArray(parameter)), value);
        transaction.RefBlockNumber = refBlockInfo.Height;
        transaction.RefBlockPrefix = refBlockInfo.Prefix;

        var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);
        return transaction;
    }

    private async Task<Transaction> GetTransactionWithoutSignatureAsync(Address from, Address to, string methodName,
        ByteString parameter = null, long value = 0)
    {
        var parameterWithValue = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value
        }.ToByteString();
        var registration = await BasicContractZeroStub.GetSmartContractRegistrationByAddress.CallAsync(to);
        if (registration.Category == 0)
        {
            throw new Exception("Registration not found.");
        }
        var wasmCode = new WasmContractCode();
        wasmCode.MergeFrom(registration.Code);
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(wasmCode.Abi);
        var selector = methodName == "deploy" ? solangAbi.GetConstructor() : solangAbi.GetSelector(methodName);
        var transaction = new Transaction
        {
            From = from,
            To = to,
            MethodName = selector,
            Params = parameterWithValue
        };

        return transaction;
    }

    internal static string ExtraContractCodeFromHardhatOutput(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var hardhatOutput = JsonSerializer.Deserialize<HardhatOutput>(json);
        var contracts = hardhatOutput.Input.Sources.Select(s => s.Value.Values).Select(v => v.First()).ToList();
        return contracts.IntegrateContracts();
    }

    internal async Task<ByteString> QueryField(Address contractAddress, string fieldName, ByteString parameter = null)
    {
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, fieldName, parameter);
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        return txResult.ReturnValue;
    }

    internal async Task<WasmContractCode> LoadWasmContractCode(string contractPath)
    {
        var abi = await File.ReadAllTextAsync(contractPath);
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(abi);
        var code = solangAbi.Source.Wasm.HexToByteArray();
        var wasmCode = new WasmContractCode
        {
            Code = ByteString.CopyFrom(code),
            Abi = abi,
            CodeHash = Hash.LoadFromHex(solangAbi.Source.Hash)
        };
        return wasmCode;
    }
}