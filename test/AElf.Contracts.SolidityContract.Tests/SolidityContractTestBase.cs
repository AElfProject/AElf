using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
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
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal async Task<IExecutionResult<Address>> DeployWebAssemblyContractAsync(byte[] codeBytes)
    {
        return await BasicContractZeroStub.DeploySmartContract.SendAsync(new ContractDeploymentInput
        {
            Category = KernelConstants.WebAssemblyRunnerCategory,
            Code = ByteString.CopyFrom(codeBytes)
        });
    }

    internal Transaction GetTransaction(ECKeyPair keyPair, Address to, string methodName,
        ByteString parameter = null)
    {
        var refBlockInfo = RefBlockInfoProvider.GetRefBlockInfo();
        var transaction =
            GetTransactionWithoutSignature(Address.FromPublicKey(keyPair.PublicKey), to, methodName, parameter);
        transaction.RefBlockNumber = refBlockInfo.Height;
        transaction.RefBlockPrefix = refBlockInfo.Prefix;

        var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);
        return transaction;
    }

    private Transaction GetTransactionWithoutSignature(Address from, Address to, string methodName,
        ByteString parameter)
    {
        var transaction = new Transaction
        {
            From = from,
            To = to,
            MethodName = methodName,
            Params = parameter ?? ByteString.Empty
        };

        return transaction;
    }
}