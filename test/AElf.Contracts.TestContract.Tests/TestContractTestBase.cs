using System.IO;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.Basic1;
using AElf.Contracts.TestContract.Basic11;
using AElf.Contracts.TestContract.Basic2;
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
        protected readonly Hash TestBasic1ContractSystemName = Hash.FromString("AElf.ContractNames.TestContract.Basic1");
        protected readonly Hash TestBasic2ContractSystemName = Hash.FromString("AElf.ContractNames.TestContract.Basic2");
        
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address Basic1ContractAddress { get; set; }
        protected Address Basic2ContractAddress { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        
        internal Basic1ContractContainer.Basic1ContractStub TestBasic1ContractStub { get; set; }
        
        internal Basic2ContractContainer.Basic2ContractStub TestBasic2ContractStub { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal Basic1ContractContainer.Basic1ContractStub GetTestBasic1ContractStub(ECKeyPair keyPair)
        {
            return GetTester<Basic1ContractContainer.Basic1ContractStub>(Basic1ContractAddress, keyPair);
        }
        
        internal Basic11ContractContainer.Basic11ContractStub GetTestBasic11ContractStub(ECKeyPair keyPair)
        {
            return GetTester<Basic11ContractContainer.Basic11ContractStub>(Basic1ContractAddress, keyPair);
        }
        
        internal Basic2ContractContainer.Basic2ContractStub GetTestBasic2ContractStub(ECKeyPair keyPair)
        {
            return GetTester<Basic2ContractContainer.Basic2ContractStub>(Basic2ContractAddress, keyPair);
        }

        protected void InitializeTestContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);
            
            //deploy test contract1
            Basic1ContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(Basic1Contract).Assembly.Location)),
                        Name = TestBasic1ContractSystemName,
                        TransactionMethodCallList = GenerateTestBasic1InitializationCallList()
                    })).Output;
            TestBasic1ContractStub = GetTestBasic1ContractStub(DefaultSenderKeyPair);
            
            //deploy test contract2
            Basic2ContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(Basic2Contract).Assembly.Location)),
                        Name = TestBasic2ContractSystemName,
                        TransactionMethodCallList = GenerateTestBasic2InitializationCallList()
                    })).Output;
            TestBasic2ContractStub = GetTestBasic2ContractStub(DefaultSenderKeyPair);
        }

        private SystemTransactionMethodCallList GenerateTestBasic1InitializationCallList()
        {
            var basic1CallList = new SystemTransactionMethodCallList();
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
        
        private SystemTransactionMethodCallList GenerateTestBasic2InitializationCallList()
        {
            var basic2CallList = new SystemTransactionMethodCallList();
            basic2CallList.Add(nameof(Basic2Contract.InitialBasic2Contract),
                Basic1ContractAddress);

            return basic2CallList;
        }
    }
}