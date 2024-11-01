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
using AElf.Runtime.WebAssembly.Extensions;
using AElf.SolidityContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util.ByteArrayConvertors;
using Shouldly;
using Solang;
using Solang.Extensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;
using Transaction = AElf.Types.Transaction;

namespace AElf.Contracts.SolidityContract;

public class SolidityContractTestBase : ContractTestBase<SolidityContractTestAElfModule>
{
    private readonly ITestOutputHelper _outputHelper;
    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    protected virtual string ContractPath { get; set; }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

    internal readonly IBlockchainService BlockchainService;
    internal readonly ISmartContractAddressService SmartContractAddressService;
    internal readonly ITestTransactionExecutor TestTransactionExecutor;
    internal readonly IRefBlockInfoProvider RefBlockInfoProvider;

    public SolidityContractTestBase(ITestOutputHelper outputHelper = null)
    {
        _outputHelper = outputHelper;
        SmartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        BlockchainService = GetRequiredService<IBlockchainService>();
        TestTransactionExecutor = GetRequiredService<ITestTransactionExecutor>();
        RefBlockInfoProvider = GetRequiredService<IRefBlockInfoProvider>();
        InitializeContracts();
    }

    private void InitializeContracts()
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
    
    protected async Task<Address> DeployContractAsync(ByteString input = null)
    {
        _outputHelper.WriteLine("Deploying Contract: " + ContractPath);
        var wasmCode = await LoadWasmContractCode(ContractPath);
        var executionResult = await DeployWasmContractAsync(wasmCode, input);
        var txResult = executionResult.TransactionResult;
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.Logs.Count.ShouldBePositive();
        _outputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.GetPrints())
        {
            _outputHelper.WriteLine(print);
        }

        _outputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.GetRuntimeLogs())
        {
            _outputHelper.WriteLine(runtimeLog);
        }

        _outputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.GetDebugMessages())
        {
            _outputHelper.WriteLine(debugMessage);
        }

        _outputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.GetErrorMessages())
        {
            _outputHelper.WriteLine(errorMessage);
        }
        return executionResult.Output;
    }

    protected async Task<Hash> UploadContractAsync(string contractPath)
    {
        var wasmCode = await LoadWasmContractCode(contractPath);
        var executionResult = await UploadWasmContractAsync(wasmCode);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return executionResult.Output;
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
    
    internal async Task<IExecutionResult<Hash>> UploadWasmContractAsync(WasmContractCode wasmCode)
    {
        var executionResult = await BasicContractZeroStub.UploadSoliditySmartContract.SendAsync(
            new UploadSoliditySmartContractInput
            {
                Category = KernelConstants.WasmRunnerCategory,
                Code = wasmCode.ToByteString(),
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
        if (registration.Code.IsNullOrEmpty())
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
    
    internal async Task<ByteString> ReadAsync(Address contractAddress, string fieldName, ByteString parameter = null)
    {
        _outputHelper.WriteLine("Executing read: " + fieldName);

        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, fieldName, parameter);
        return await TestTransactionExecutor.ReadAsync(tx);
    }

    internal async Task<ByteString> QueryAsync(Address contractAddress, string fieldName, ByteString parameter = null)
    {
        _outputHelper.WriteLine("\nExecuting query: " + fieldName);

        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, fieldName, parameter);
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        //txResult.Status.ShouldBe(TransactionResultStatus.Mined);
 
        _outputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.GetPrints())
        {
            _outputHelper.WriteLine(print);
        }

        _outputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.GetRuntimeLogs())
        {
            _outputHelper.WriteLine(runtimeLog);
        }

        _outputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.GetDebugMessages())
        {
            _outputHelper.WriteLine(debugMessage);
        }

        _outputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.GetErrorMessages())
        {
            _outputHelper.WriteLine(errorMessage);
        }

        return txResult.ReturnValue;
    }

    internal async Task<ByteString> QueryWithExceptionAsync(Address contractAddress, string fieldName,
        ByteString parameter = null)
    {
        _outputHelper.WriteLine("\nExecuting query: " + fieldName);

        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, fieldName, parameter);
        var txResult = await TestTransactionExecutor.ExecuteWithExceptionAsync(tx);
        txResult.Status.ShouldNotBe(TransactionResultStatus.Mined);
        _outputHelper.WriteLine(txResult.Error);

        _outputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.GetPrints())
        {
            _outputHelper.WriteLine(print);
        }

        _outputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.GetRuntimeLogs())
        {
            _outputHelper.WriteLine(runtimeLog);
        }

        _outputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.GetDebugMessages())
        {
            _outputHelper.WriteLine(debugMessage);
        }

        _outputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.GetErrorMessages())
        {
            _outputHelper.WriteLine(errorMessage);
        }

        return txResult.ReturnValue;
    }

    internal async Task<TransactionResult> ExecuteTransactionAsync(Address contractAddress, string functionName,
        ByteString parameter = null, long value = 0)
    {
        _outputHelper.WriteLine("Executing tx: " + functionName);

        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, functionName, parameter, value);
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        _outputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.GetPrints())
        {
            _outputHelper.WriteLine(print);
        }

        _outputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.GetRuntimeLogs())
        {
            _outputHelper.WriteLine(runtimeLog);
        }

        _outputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.GetDebugMessages())
        {
            _outputHelper.WriteLine(debugMessage);
        }

        _outputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.GetErrorMessages())
        {
            _outputHelper.WriteLine(errorMessage);
        }

        return txResult;
    }
    
    internal async Task<TransactionResult> ExecuteTransactionWithExceptionAsync(Address contractAddress, string functionName,
        ByteString parameter = null, long value = 0)
    {
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, functionName, parameter, value);
        var txResult = await TestTransactionExecutor.ExecuteWithExceptionAsync(tx);
        txResult.Status.ShouldNotBe(TransactionResultStatus.Mined);
        return txResult;
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