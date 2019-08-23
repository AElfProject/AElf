using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.TestKit;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.CrossChain.Tests
{
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SideChainLifeTimeManagementTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        #region Side Chain Action

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

            var parentChain = await CrossChainContractStub.GetParentChainId.CallAsync(new Empty());
            Assert.True(parentChain.Equals(new SInt32Value {Value = ChainHelper.ConvertBase58ToChainId("AELF")}));
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

            var balance = (await CrossChainContractStub.LockedBalance.CallAsync(new SInt32Value {Value = chainId}))
                .Value;
            Assert.Equal(10, balance);
        }

        [Fact]
        public async Task CheckLockedBalance_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult = (await CrossChainContractStub.LockedBalance.SendAsync(new SInt32Value {Value = chainId}))
                .TransactionResult;
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", txResult.Error);
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

            var txResult = (await crossChainContractStub2.LockedBalance.SendAsync(new SInt32Value {Value = chainId}))
                .TransactionResult;
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Unable to check balance.", txResult.Error);
        }

        [Fact]
        public async Task GetLockedToken()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var lockedToken = await CrossChainContractStub.LockedToken.CallAsync(new SInt32Value
            {
                Value = sideChainId
            });
            Assert.True(lockedToken.Value == lockedTokenAmount);
        }

        [Fact]
        public async Task GetLockedToken_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.LockedToken.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });

            Assert.True(res.TransactionResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task GetLockedToken_DisposedChain()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = sideChainId
            });
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var res = await CrossChainContractStub.LockedToken.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Disposed side chain.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task GetLockedAddress()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var address = await CrossChainContractStub.LockedAddress.CallAsync(new SInt32Value
            {
                Value = sideChainId
            });

            Assert.True(address == DefaultSender);
        }

        [Fact]
        public async Task GetLockedAddress_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.LockedAddress.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });
            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task GetLockedAddress_DisposedChain()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = sideChainId
            });
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var res = await CrossChainContractStub.LockedToken.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Disposed side chain.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task RechargeForSideChain()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };

            //without enough token
            var res = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);
            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Insufficient allowance", res.TransactionResult.Error);

            //with enough token
            await ApproveBalanceAsync(100_000L);
            res = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);
            status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RechargeForSideChain_Terminated()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            await ApproveBalanceAsync(100_000L);

            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value {Value = sideChainId});
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value
            {
                Value = sideChainId
            });

            Assert.True(chainStatus.Value == (int) SideChainStatus.Terminated);
            var res = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found or not able to be recharged.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task RechargeForSideChain_ChainNoExist()
        {
            var parentChainId = 123;
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync(parentChainId);

            await ApproveBalanceAsync(lockedTokenAmount);
            var otherChainId = ChainHelper.GetChainId(5);
            var rechargeInput = new RechargeInput()
            {
                ChainId = otherChainId,
                Amount = 100_000L
            };
            await ApproveBalanceAsync(100_000L);
            var res = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found or not able to be recharged.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task Disposal_SideChain()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);
            var chainId = await InitAndCreateSideChainAsync();

            var balanceAfterCreate = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = CrossChainContractAddress,
                Symbol = "ELF"
            });
            Assert.True(balanceAfterCreate.Balance == lockedTokenAmount);

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

            var balanceAfterDisposal = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = CrossChainContractAddress,
                Symbol = "ELF"
            });
            Assert.True(balanceAfterDisposal.Balance == 0);
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
            var res = await parliamentAuthContractStub2.Release.SendAsync(proposalId);

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to dispose.", res.TransactionResult.Error);
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
            var res = await CrossChainContractStub.GetChainStatus.SendAsync(new SInt32Value {Value = chainId});
            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        #endregion

        [Fact]
        public async Task ChangeOwnerAddress()
        {
            var organizationAddress = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposalRes = await ParliamentAuthContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "ChangOwnerAddress",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = DefaultSender.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            });

            var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);
            
            var res = await CrossChainContractStub.ChangOwnerAddress.SendAsync(DefaultSender);

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeOwnerAddress_NotAuthorized()
        {
            var res = (await CrossChainContractStub.ChangOwnerAddress.SendAsync(DefaultSender)).TransactionResult;

            var status = res.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", res.Error);
        }
    }
}