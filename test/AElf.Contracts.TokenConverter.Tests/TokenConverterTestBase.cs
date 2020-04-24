using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterTestBase : AEDPoSExtensionTestBase
    {
        #region Contract Address

        protected Address TokenContractAddress =>
            ContractAddresses[TokenSmartContractAddressNameProvider.Name];

        protected Address TreasuryContractAddress =>
            ContractAddresses[TreasurySmartContractAddressNameProvider.Name];

        protected Address TokenConverterContractAddress =>
            ContractAddresses[TokenConverterSmartContractAddressNameProvider.Name];

        protected Address ParliamentContractAddress =>
            ContractAddresses[ParliamentSmartContractAddressNameProvider.Name];

        #endregion

        #region Stubs

        internal TokenContractContainer.TokenContractStub TokenContractStub =>
            GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

        internal TokenContractContainer.TokenContractStub AuthorizedTokenContractStub =>
            GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, ManagerKeyPair);

        internal TokenConverterContractContainer.TokenConverterContractStub DefaultStub =>
            GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                DefaultSenderKeyPair);

        internal TokenConverterContractContainer.TokenConverterContractStub AuthorizedTokenConvertStub =>
            GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                ManagerKeyPair);

        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub =>
            GetParliamentContractTester(DefaultSenderKeyPair);

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        #endregion

        #region Properties

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address FeeReceiverAddress => TreasuryContractAddress;
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs => SampleECKeyPairs.KeyPairs.Take(5).ToList();

        #endregion

        public TokenConverterTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                TreasurySmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
            }));

            AsyncHelper.RunSync(InitializeTokenAsync);
        }

        protected async Task<long> GetBalanceAsync(string symbol, Address owner)
        {
            var balanceResult = await TokenContractStub.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = owner,
                    Symbol = symbol
                });
            return balanceResult.Balance;
        }

        private async Task InitializeTokenAsync()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput()
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000_0000L,
                Issuer = DefaultSender,
                LockWhiteList = {TokenConverterContractAddress}
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Set for token converter."
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 100_0000_0000L,
                To = ManagerAddress,
                Memo = "Set for token converter."
            });
        }

        protected async Task InitializeParliamentContractAsync()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput()
            {
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });
            CheckResult(initializeResult.TransactionResult);
        }

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
    }
}