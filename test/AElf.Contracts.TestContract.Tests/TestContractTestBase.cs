using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contract.TestContract
{
    public class TestContractTestBase : ContractTestBase<TestContractAElfModule>
    {
        protected readonly Hash TestBasicFunctionContractSystemName =
            Hash.FromString("AElf.ContractNames.TestContract.BasicFunction");

        protected readonly Hash TestBasicSecurityContractSystemName =
            Hash.FromString("AElf.ContractNames.TestContract.BasicSecurity");

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address BasicFunctionContractAddress { get; set; }
        protected Address BasicSecurityContractAddress { get; set; }

        internal Acs0.ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub TestBasicFunctionContractStub { get; set; }

        internal BasicSecurityContractContainer.BasicSecurityContractStub TestBasicSecurityContractStub { get; set; }

        internal Acs0.ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<Acs0.ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        internal BasicFunctionContractContainer.BasicFunctionContractStub GetTestBasicFunctionContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress,
                keyPair);
        }

        internal BasicUpdateContractContainer.BasicUpdateContractStub GetTestBasicUpdateContractStub(ECKeyPair keyPair)
        {
            return GetTester<BasicUpdateContractContainer.BasicUpdateContractStub>(BasicFunctionContractAddress,
                keyPair);
        }

        internal BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
            GetTestBasicFunctionWithParallelContractStub(ECKeyPair keyPair)
        {
            return GetTester<BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub>(
                BasicFunctionContractAddress,
                keyPair);
        }

        internal BasicSecurityContractContainer.BasicSecurityContractStub GetTestBasicSecurityContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<BasicSecurityContractContainer.BasicSecurityContractStub>(BasicSecurityContractAddress,
                keyPair);
        }

        protected void InitializeTestContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy test contract1
            BasicFunctionContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value,
                    TestBasicFunctionContractSystemName,
                    DefaultSenderKeyPair));
            TestBasicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitialBasicFunctionContract());

            //deploy test contract2
            BasicSecurityContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    Codes.Single(kv => kv.Key.EndsWith("BasicSecurity")).Value,
                    TestBasicSecurityContractSystemName,
                    DefaultSenderKeyPair));
            TestBasicSecurityContractStub = GetTestBasicSecurityContractStub(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeSecurityContract());
        }

        private async Task InitialBasicFunctionContract()
        {
            await TestBasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput()
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey)
                });
        }

        private async Task InitializeSecurityContract()
        {
            await TestBasicSecurityContractStub.InitialBasicSecurityContract.SendAsync(BasicFunctionContractAddress);
        }
    }
}