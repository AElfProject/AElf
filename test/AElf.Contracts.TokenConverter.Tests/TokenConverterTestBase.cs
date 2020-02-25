using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterTestBase:ContractTestBase<TokenConverterTestModule>
    {
        protected Address TokenContractAddress;
        protected Address TreasuryContractAddress;
        protected Address TokenConverterContractAddress;

        internal TokenContractContainer.TokenContractStub TokenContractStub;
        internal TokenContractContainer.TokenContractStub AuthorizedTokenContractStub;
        
        internal TokenConverterContractContainer.TokenConverterContractStub DefaultStub;
        internal TokenConverterContractContainer.TokenConverterContractStub AuthorizedTokenConvertStub;
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub;
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address FeeReceiverAddress => TreasuryContractAddress;
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected Address ParliamentContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs => SampleECKeyPairs.KeyPairs.Take(5).ToList();

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        protected async Task DeployContractsAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            {
                // TokenContract
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code, TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
                AuthorizedTokenContractStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, ManagerKeyPair);
            }
            {
                // TokenConverterContract
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("TokenConverter")).Value;
                TokenConverterContractAddress = await DeploySystemSmartContract(category, code, TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                DefaultStub = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                    TokenConverterContractAddress, DefaultSenderKeyPair);
                AuthorizedTokenConvertStub = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                    TokenConverterContractAddress, ManagerKeyPair);
            }
            {
                // TreasuryContract
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Treasury")).Value;
                TreasuryContractAddress = await DeploySystemSmartContract(category, code, TreasurySmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            }
            //ParliamentContract
            {
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Parliament")).Value;
                ;
                ParliamentContractAddress = await DeploySystemSmartContract(category, code,
                    ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                ParliamentContractStub =
                    GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                        DefaultSenderKeyPair);
            }
            //AEDPOSContract
            {
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Consensus.AEDPoS")).Value;
                ConsensusContractAddress = await DeploySystemSmartContract(category, code,
                    Hash.FromString("AElf.ContractNames.Consensus"), DefaultSenderKeyPair);
                AEDPoSContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
            }
            
            await TokenContractStub.Create.SendAsync(new CreateInput()
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000_0000L,
                Issuer = DefaultSender,
                LockWhiteList = { TokenConverterContractAddress} 
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

        protected async Task InitializeParliamentContractAsync()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput()
            {
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });
            CheckResult(initializeResult.TransactionResult);
        }

        protected async Task InitializeAElfConsensusAsync()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        TimeEachTerm = 604800L,
                        MinerIncreaseInterval = 31536000
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));
                CheckResult(result.TransactionResult);
            }
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