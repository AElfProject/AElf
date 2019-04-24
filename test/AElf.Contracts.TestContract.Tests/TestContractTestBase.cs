using System.IO;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contract.TestContract
{
    public class TestContractTestBase : ContractTestBase<TestContractAElfModule>
    {
        protected readonly Hash TestBasicFunctionContractSystemName = Hash.FromString("AElf.ContractNames.TestContract.BasicFunction");
        protected readonly Hash TestBasicSecurityContractSystemName = Hash.FromString("AElf.ContractNames.TestContract.BasicSecurity");
        
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address BasicFunctionContractAddress { get; set; }
        protected Address BasicSecurityContractAddress { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        
        internal BasicFunctionContractContainer.BasicFunctionContractStub TestBasicFunctionContractStub { get; set; }
        
        internal BasicSecurityContractContainer.BasicSecurityContractStub TestBasicSecurityContractStub { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal BasicFunctionContractContainer.BasicFunctionContractStub GetTestBasicFunctionContractStub(ECKeyPair keyPair)
        {
            return GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress, keyPair);
        }
        
        internal BasicUpdateContractContainer.BasicUpdateContractStub GetTestBasicUpdateContractStub(ECKeyPair keyPair)
        {
            return GetTester<BasicUpdateContractContainer.BasicUpdateContractStub>(BasicFunctionContractAddress, keyPair);
        }
        
        internal BasicSecurityContractContainer.BasicSecurityContractStub GetTestBasicSecurityContractStub(ECKeyPair keyPair)
        {
            return GetTester<BasicSecurityContractContainer.BasicSecurityContractStub>(BasicSecurityContractAddress, keyPair);
        }

        protected void InitializeTestContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);
            
            //deploy test contract1
            BasicFunctionContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicFunctionContract).Assembly.Location)),
                        Name = TestBasicFunctionContractSystemName,
                        TransactionMethodCallList = GenerateTestBasicFunctionInitializationCallList()
                    })).Output;
            TestBasicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
            
            //deploy test contract2
            BasicSecurityContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicSecurityContract).Assembly.Location)),
                        Name = TestBasicSecurityContractSystemName,
                        TransactionMethodCallList = GenerateTestBasicSecurityInitializationCallList()
                    })).Output;
            TestBasicSecurityContractStub = GetTestBasicSecurityContractStub(DefaultSenderKeyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTestBasic1InitializationCallList()
        {
            var basic1CallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            basic1CallList.Add(nameof(Basic1Contract.InitialBasic1Contract),
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey)
                });

            return basic1CallList;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTestBasic2InitializationCallList()
        {
            var basic2CallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            basic2CallList.Add(nameof(Basic2Contract.InitialBasic2Contract),
                Basic1ContractAddress);

            return basic2CallList;
        }
    }
}