using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.CrossChain.AEDPos.Tests
{
    public class CrossChainContractTestBase : AEDPoSExtensionTestBase
    {
        #region Contract Address

        public Address TokenContractAddress => 
            ContractAddresses[TokenSmartContractAddressNameProvider.Name];

        protected Address ParliamentAuthContractAddress =>
            ContractAddresses[ParliamentAuthSmartContractAddressNameProvider.Name];
        public Address CrossChainContractAddress => 
            ContractAddresses[CrossChainSmartContractAddressNameProvider.Name];

        public Address TokenConverterContractAddress =>
            ContractAddresses[TokenConverterSmartContractAddressNameProvider.Name];
        #endregion
        
        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address CallOwner => Address.FromPublicKey(DefaultKeyPair.PublicKey);

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                DefaultKeyPair);

        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);
        
        internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                keyPair);
        }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub =>
            GetTokenConverterContractTester(DefaultKeyPair);

        #region Paliament

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub =>
            GetParliamentAuthContractTester(DefaultKeyPair);
        
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
                keyPair);
        }

        #endregion
        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(CrossChainContractTestConstants.InitialCoreDataCenterCount).ToList();

        internal CrossChainContractContainer.CrossChainContractStub CrossChainStub =>
            GetTester<CrossChainContractContainer.CrossChainContractStub>(
                ContractAddresses[CrossChainSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        public CrossChainContractTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                ParliamentAuthSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitializeParliamentContractAsync);
        }
        
        protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainStub.Initialize.GetTransaction(new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                })
            });
        }

        protected async Task InitializeParliamentContractAsync()
        {
            var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new ParliamentAuth.InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
                PrivilegedProposer = CallOwner,
                ProposerAuthorityRequired = true
            });
            CheckResult(initializeResult.TransactionResult);
        }

        protected async Task ApproveBalanceAsync(long amount)
        {
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                TokenStub.Create.GetTransaction(new CreateInput
                {
                    Symbol = "ELF",
                    TokenName = "ELF",
                    TotalSupply = 100,
                    Decimals = 2,
                    Issuer = CallOwner,
                    IsBurnable = true,
                    LockWhiteList = {CrossChainContractAddress}
                    
                }),
                TokenStub.Issue.GetTransaction(new IssueInput
                {
                    Symbol = "ELF",
                    Amount = 100,
                    Memo = "ELF",
                    To = CallOwner
                }),
                TokenStub.Approve.GetTransaction(new MultiToken.Messages.ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                }),
                TokenStub.GetAllowance.GetTransaction(new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = CallOwner,
                    Spender = CrossChainContractAddress
                })
            });
        }
        
        internal async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            //var minierList = ConsensusStub.GetCurrentMinerList.SendAsync(new Empty());
            
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount, contractCode);
            var organizationAddress = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposal = ParliamentAuthContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "CreateSideChain",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = createProposalInput.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            }).Result.TransactionResult;
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId)
        {
//            var res = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
//            {
//                ProposalId = proposalId
//            });
//            var result = res.TransactionResult;
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                ParliamentAuthContractStub.Approve.GetTransaction(new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                }),
                ParliamentAuthContractStub.Approve.GetTransaction(new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                })
            });
        }
        
        protected async Task ReleaseProposalAsync(Hash proposalId)
        {
            var transaction = ParliamentAuthContractStub.Release.GetTransaction(proposalId);
            await BlockMiningService.MineBlockAsync(new List<Transaction>{transaction});
        }
        
        internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var res = new SideChainCreationRequest
            {
                ContractCode = contractCode,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount
            };
//            if (resourceTypeBalancePairs != null)
//                res.ResourceBalances.AddRange(resourceTypeBalancePairs.Select(x =>
//                    ResourceTypeBalancePair.Parser.ParseFrom(x.ToByteString())));
            return res;
        }

        private SideChainBlockData CreateSideChainBlockData(Hash blockHash, long height, int sideChainId,
            Hash txMerkleTreeRoot)
        {
            return new SideChainBlockData
            {
                BlockHeaderHash = blockHash,
                Height = height,
                ChainId = sideChainId,
                TransactionMerkleTreeRoot = txMerkleTreeRoot
            };
        }

        private async Task SetConnector(Connector connector)
        {
            var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = connectorManagerAddress,
                ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = connector.ToByteString(),
                ToAddress = TokenConverterContractAddress
            };
            var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
            CheckResult(createResult.TransactionResult);

            var proposalHash = Hash.FromMessage(proposal);
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentAuthContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(new Acs3.ApproveInput
                {
                    ProposalId = proposalHash,
                });
                CheckResult(approveResult.TransactionResult);
            }

            var releaseResult = await ParliamentAuthContractStub.Release.SendAsync(proposalHash);
            CheckResult(releaseResult.TransactionResult);
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