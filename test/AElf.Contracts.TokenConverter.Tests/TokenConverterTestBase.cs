using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.Kernel.Consensus;
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

        internal TokenConverterContractImplContainer.TokenConverterContractImplStub DefaultStub =>
            GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(TokenConverterContractAddress,
                DefaultSenderKeyPair);
        
        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
            GetParliamentContractTester(DefaultSenderKeyPair);
        
        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                keyPair);
        }

        #endregion

        #region Properties

        protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
        protected Address DefaultSender => Accounts[0].Address;
        protected Address FeeReceiverAddress => TreasuryContractAddress;
        protected ECKeyPair ManagerKeyPair => Accounts[11].KeyPair;
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs => Accounts.Take(5).Select(a => a.KeyPair).ToList();

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