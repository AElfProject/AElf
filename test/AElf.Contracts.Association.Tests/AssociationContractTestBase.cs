using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Association
{
    public class AssociationContractTestBase<T> : ContractTestBase<T> where T : AbpModule
    {
        protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
        protected ECKeyPair Reviewer1KeyPair => Accounts[1].KeyPair;
        protected ECKeyPair Reviewer2KeyPair => Accounts[2].KeyPair;
        protected ECKeyPair Reviewer3KeyPair => Accounts[3].KeyPair;
        protected Address DefaultSender => Accounts[0].Address;
        protected Address Reviewer1 => Accounts[1].Address;
        protected Address Reviewer2 => Accounts[2].Address;
        protected Address Reviewer3 => Accounts[3].Address;

        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub { get; }
        internal AssociationContractImplContainer.AssociationContractImplStub AssociationContractStub { get; }
        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; }

        public AssociationContractTestBase()
        {
            AssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);

            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);

            ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        }

        internal AssociationContractImplContainer.AssociationContractImplStub GetAssociationContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress, keyPair);
        }

        internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }

        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                keyPair);
        }
    }
}