using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.CrossChain.AEDPos.Tests
{
    public class SideChainLifeTimeTest : CrossChainContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SideChainLifeTimeTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Create_SideChain()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            // Create proposal and approve
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);

            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;
            var creator = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).Creator;
            Assert.True(creator == DefaultSender);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = chainId});
            Assert.True(chainStatus.Value == (int) SideChainStatus.Active);
        }

        [Fact]
        public async Task Create_SideChain_Failed()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            {
                var proposalId =
                    await CreateSideChainProposalAsync(10, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await ReleaseProposalAsync(proposalId);
                var status = transactionResult.Status;
                Assert.True(status == TransactionResultStatus.Failed);
                Assert.Contains("Invalid chain creation request.", transactionResult.Error);
            }
            {
                var proposalId = await CreateSideChainProposalAsync(10, 0, ByteString.CopyFromUtf8("Test"));
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await ReleaseProposalAsync(proposalId);
                Assert.True(transactionResult.Status == TransactionResultStatus.Failed);
                Assert.Contains("Invalid chain creation request.", transactionResult.Error);
            }
            {
                var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.Empty);
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await ReleaseProposalAsync(proposalId);
                Assert.True(transactionResult.Status == TransactionResultStatus.Failed);
                Assert.Contains("Invalid chain creation request.", transactionResult.Error);
            }
        }

        [Fact]
        public async Task Create_SideChain_NotAuthorized()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);

            var chainId = ChainHelper.GetChainId(5);

            var result = await CrossChainContractStub.CreateSideChain.SendAsync(
                CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test")));
            var status = result.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", result.TransactionResult.Error);
        }
        
        [Fact]
        public async Task CheckLockedBalance()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            var balance = (await CrossChainContractStub.LockedBalance.CallAsync(new SInt32Value {Value = chainId})).Value;
            Assert.Equal(10, balance);
        }
        
        [Fact]
        public async Task CheckLockedBalance_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult = (await CrossChainContractStub.LockedBalance.SendAsync(new SInt32Value {Value = chainId})).TransactionResult;
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not existed side chain.", txResult.Error);
        }
        
        [Fact]
        public async Task CheckLockedBalance_NotAuthorized()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            var ecKeyPair = SampleECKeyPairs.KeyPairs[1];
            var crossChainContractStub2 = GetCrossChainContractStub(ecKeyPair);
            
            var txResult = (await crossChainContractStub2.LockedBalance.SendAsync(new SInt32Value {Value = chainId})).TransactionResult;
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Unable to check balance.", txResult.Error);
        }
        
        [Fact]
        public async Task Disposal_SideChain()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);
            var chainId = await InitAndCreateSideChainAsync();
            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = chainId});
            Assert.True(chainStatus.Value == (int) SideChainStatus.Terminated);
        }

        [Fact]
        public async Task Disposal_SideChain_NotAuthorized()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);
            var chainId = await InitAndCreateSideChainAsync();
            var disposalInput = new SInt32Value
            {
                Value = chainId
            };

            var ecKeyPair = SampleECKeyPairs.KeyPairs[1];
            var parliamentAuthContractStub2 = GetParliamentAuthContractTester(ecKeyPair);
            var organizationAddress = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposal = await parliamentAuthContractStub2.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "DisposeSideChain",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = disposalInput.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            });
                
            var proposalId = Hash.Parser.ParseFrom(proposal.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var result = await parliamentAuthContractStub2.Release.SendAsync(proposalId);
            
            var status = result.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to dispose.", result.TransactionResult.Error);
        }
        
        [Fact]
        public async Task Disposal_SideChain__NotFound()
        {
            await InitializeCrossChainContractAsync();
            var chainId = ChainHelper.GetChainId(1);
            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var status = transactionResult.Status;

            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not existed side chain.", transactionResult.Error);
        }
        
        [Fact]
        public async Task Disposal_SideChain_WrongStatus()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);
            var chainId = await InitAndCreateSideChainAsync();
            var proposalId1 = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId1);
            var transactionResult1 = await ReleaseProposalAsync(proposalId1);
            var status1 = transactionResult1.Status;
            Assert.True(status1 == TransactionResultStatus.Mined);

            var proposalId2 = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId2);
            var transactionResult2 = await ReleaseProposalAsync(proposalId2);
            var status2 = transactionResult2.Status;
            Assert.True(status2 == TransactionResultStatus.Failed);
            Assert.Contains("Unable to dispose this side chain.", transactionResult2.Error);
        }

        [Fact]
        public async Task GetChainStatus_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult = await CrossChainContractStub.GetChainStatus.SendAsync(new SInt32Value{Value = chainId});
            var status = txResult.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not existed side chain.", txResult.TransactionResult.Error);
        }
        
        [Fact]
        public async Task Get_SideChain_Height()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;
            
            var txResult1 = await CrossChainContractStub.GetSideChainHeight.SendAsync(new SInt32Value {Value = chainId});
            var status = txResult1.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var actual = new SInt32Value();
            actual.MergeFrom(transactionResult.ReturnValue);
            Assert.Equal(0, actual.Value);
        }
        
        [Fact]
        public async Task Get_SideChain_Height_NotExist()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var chainId = ChainHelper.GetChainId(1);
            var txResult = await CrossChainContractStub.GetSideChainHeight.SendAsync(new SInt32Value {Value = chainId});
            var status = txResult.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", txResult.TransactionResult.Error);
        }
    }
}