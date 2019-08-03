using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using ApproveInput = AElf.Contracts.MultiToken.Messages.ApproveInput;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : TestKit.ContractTestBase<MultiTokenContractTestAElfModule>
    {
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub;
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair User1KeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address User1Address => Address.FromPublicKey(User1KeyPair.PublicKey);
        protected ECKeyPair User2KeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address User2Address => Address.FromPublicKey(User2KeyPair.PublicKey);
        protected const string DefaultSymbol = "ELF";
    }

    public class
        MultiTokenContractCrossChainTestBase : TestBase.ContractTestBase<MultiTokenContractCrossChainTestAElfModule>
    {
        protected Address BasicContractZeroAddress;
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ParliamentAddress;
        
        protected Address SideBasicContractZeroAddress;
        protected Address SideCrossChainContractAddress;
        protected Address SideTokenContractAddress;
        protected Address SideParliamentAddress;

        protected long TotalSupply;
        protected long BalanceOfStarter;
        protected bool IsPrivilegePreserved;
        
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> MainChainTester;
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> SideChainTester;

        protected int MainChainId;
        public MultiTokenContractCrossChainTestBase()
        {
            MainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            MainChainTester = new ContractTester<MultiTokenContractCrossChainTestAElfModule>(MainChainId,SampleECKeyPairs.KeyPairs[1]);
            AsyncHelper.RunSync(() =>
                MainChainTester.InitialChainAsyncWithAuthAsync(MainChainTester.GetDefaultContractTypes(MainChainTester.GetCallOwnerAddress(), out TotalSupply,
                    out _,
                    out BalanceOfStarter)));
            BasicContractZeroAddress = MainChainTester.GetZeroContractAddress();
            CrossChainContractAddress = MainChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            TokenContractAddress = MainChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ParliamentAddress = MainChainTester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
        }

        protected void StartSideChain(int chainId)
        {
            SideChainTester = new ContractTester<MultiTokenContractCrossChainTestAElfModule>(chainId,SampleECKeyPairs.KeyPairs[1]);
            AsyncHelper.RunSync(() =>
                SideChainTester.InitialSideChainAsync(chainId,SideChainTester.GetSideChainSystemContract(MainChainTester.GetCallOwnerAddress(),out TotalSupply,SideChainTester.GetCallOwnerAddress(),out IsPrivilegePreserved)));
            SideBasicContractZeroAddress = SideChainTester.GetZeroContractAddress();
            SideCrossChainContractAddress = SideChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            SideTokenContractAddress = SideChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            SideParliamentAddress = SideChainTester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
        }

        protected async Task ApproveBalanceAsync(long amount)
        {
            var callOwner = Address.FromPublicKey(MainChainTester.KeyPair.PublicKey);

            var approveResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Approve), new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetAllowance),
                new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = callOwner,
                    Spender = CrossChainContractAddress
                });
        }

        protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await MainChainTester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await MainChainTester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }
        
        protected async Task InitializeCrossChainContractOnSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await SideChainTester.GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await SideChainTester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }

        protected async Task<int> InitAndCreateSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersOnMainChainAsync(proposalId);

            var transactionResult = await ReleaseProposalAsync(proposalId,ParliamentAddress,"Main");
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            return chainId;
        }

        protected async Task<Block> MineAsync(List<Transaction> txs, string chainType)
        {
            if (chainType.Equals("Side"))
                return await SideChainTester.MineAsync(txs);
            return await MainChainTester.MineAsync(txs);
        }

        protected async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress,
            string methodName, IMessage input, string chainType)
        {
            if (chainType.Equals("Side"))
            {
                return await SideChainTester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
            }
            return await MainChainTester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input, string chainType)
        {
            if (chainType.Equals("Side"))
            {
                return ecKeyPair == null
                    ? await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                    : await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
            }
            return ecKeyPair == null
                ? await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        protected async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            return await MainChainTester.GetTransactionResultAsync(txId);
        }

        protected async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            return await MainChainTester.CallContractMethodAsync(contractAddress, methodName, input);
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

        private async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount, contractCode);
            var organizationAddress = Address.Parser.ParseFrom((await MainChainTester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "CreateSideChain",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = createProposalInput.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task ApproveWithMinersOnMainChainAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), MainChainTester.InitialMinerList[0],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Main");
            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), MainChainTester.InitialMinerList[1],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Main");
            await MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2},"Main");
        }
        
        protected async Task ApproveWithMinersOnSideChainAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), SideChainTester.InitialMinerList[0],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Side");
            var approveTransaction2 = await GenerateTransactionAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), SideChainTester.InitialMinerList[1],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Side");
            await MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2},"Side");
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId,Address parliamentAddress ,string chainType)
        {
            var transactionResult = await ExecuteContractWithMiningAsync(parliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release),proposalId,chainType);
            return transactionResult;
        }
        
        protected async Task<Hash> CreateProposalAsyncOnMainChain(string method, ByteString input, Address contractAddress)
        {
            var organizationAddress = Address.Parser.ParseFrom((await MainChainTester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = method,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = input,
                    ToAddress = contractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }
        
        protected async Task<Hash> CreateProposalAsyncOnSideChain(string method, ByteString input, Address contractAddress)
        {
            var organizationAddress = Address.Parser.ParseFrom((await SideChainTester.ExecuteContractWithMiningAsync(
                    SideParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await SideChainTester.ExecuteContractWithMiningAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = method,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = input,
                    ToAddress = contractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }
    }
}