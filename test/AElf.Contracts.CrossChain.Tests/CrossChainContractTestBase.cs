using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestBase;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using ApproveInput = AElf.Contracts.MultiToken.Messages.ApproveInput;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;
using ResourceTypeBalancePair = AElf.Contracts.CrossChain.ResourceTypeBalancePair;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainContractTestBase : ContractTestBase<CrossChainContractTestAElfModule>
    {
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ConsensusContractAddress;
        protected Address ParliamentAddress;

        protected long _totalSupply;
        protected long _balanceOfStarter;
        public CrossChainContractTestBase()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply, out _,
                    out _balanceOfStarter)));
            CrossChainContractAddress = Tester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ConsensusContractAddress = Tester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
            ParliamentAddress = Tester.GetContractAddress(ParliamentAuthContractAddressNameProvider.Name);
        }

        protected async Task ApproveBalance(long amount)
        {
            var callOwner = Address.FromPublicKey(Tester.KeyPair.PublicKey);

            var approveResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Approve), new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContractContainer.TokenContractStub.GetAllowance),
                new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = callOwner,
                    Spender = CrossChainContractAddress
                });
        }

        protected async Task InitializeCrossChainContract(long parentChainHeightOfCreation = 0, int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await Tester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelpers.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await Tester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }

        protected async Task<int> InitAndCreateSideChain(long parentChainHeightOfCreation = 0, int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await InitializeCrossChainContract(parentChainHeightOfCreation, parentChainId);

            await ApproveBalance(lockedTokenAmount);
            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            var requestTxResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
                sideChainCreationRequest);
            await ApproveWithMiners(RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ProposalId);
            var chainId = ChainHelpers.GetChainId(1);
            
            return chainId;
        }

        protected async Task<Block> MineAsync(List<Transaction> txs)
        {
            return await Tester.MineAsync(txs);
        }
        
        protected  async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName, IMessage input)
        {
            return await Tester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName, ECKeyPair ecKeyPair, IMessage input)
        {
            return ecKeyPair == null
                ? await Tester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        protected async Task<TransactionResult> GetTransactionResult(Hash txId)
        {
            return await Tester.GetTransactionResultAsync(txId);
        }

        protected async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            return await Tester.CallContractMethodAsync(contractAddress, methodName, input);
        }

        protected byte[] GetFriendlyBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes.Skip(Array.FindIndex(bytes, Convert.ToBoolean)).ToArray();
        }

        internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount, ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var res = new SideChainCreationRequest
            {
                ContractCode = contractCode,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount
            };
            if (resourceTypeBalancePairs != null)
                res.ResourceBalances.AddRange(resourceTypeBalancePairs.Select(x =>
                    ResourceTypeBalancePair.Parser.ParseFrom(x.ToByteString())));
            return res;
        }

        protected async Task ApproveWithMiners(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[0],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });
            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[1], new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });
            await Tester.MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2});
        }

        protected async Task<Hash> CreateProposal(int chainId, string methodName)
        {
            var createProposalInput = new SInt32Value
            {
                Value = chainId
            };
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetDefaultOwnerAddress), new Empty())).ReturnValue);
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal), new CreateProposalInput
                {
                    ContractMethodName = methodName,
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = createProposalInput.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }
    }
}