using System.Collections.Generic;
using System.IO;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthContractTestBase : ContractTestBase<AssociationAuthContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address Reviewer1 => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey);
        protected Address Reviewer2 => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey);
        protected Address Reviewer3 => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[3].PublicKey);
        
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address AssociationAuthContractAddress { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal AssociationAuthContractContainer.AssociationAuthContractStub AssociationAuthContractStub { get; set; }

        protected void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy AssociationAuth contract
            AssociationAuthContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySmartContract.SendAsync(
                    new ContractDeploymentInput()
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AssociationAuthContract).Assembly.Location)),  
                    })).Output;
            AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal AssociationAuthContractContainer.AssociationAuthContractStub GetAssociationAuthContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationAuthContractContainer.AssociationAuthContractStub>(AssociationAuthContractAddress, keyPair);
        }
    }
}