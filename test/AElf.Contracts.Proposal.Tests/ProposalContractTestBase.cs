using System.IO;
using AElf.Contracts.TestKit;
using AElf.Contracts.Genesis;
using AElf.Contracts.ProposalContract;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.Proposal
{
    public class ProposalContractTestBase : ContractTestBase<ProposalContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address TestAddress => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey);
        protected Address Propoer => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey); 
        protected Address ProposalContractAddress { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal ProposalContractContainer.ProposalContractStub ProposalContractStub { get; set; }

        protected void DeployContract()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);
            ProposalContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySmartContract.SendAsync(
                    new ContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProposalContract).Assembly.Location))
                    })).Output;
            ProposalContractStub = GetProposalContractTester(DefaultSenderKeyPair);
        }
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }
       
        internal ProposalContractContainer.ProposalContractStub GetProposalContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProposalContractContainer.ProposalContractStub>(ProposalContractAddress, keyPair);
        }
    }
}