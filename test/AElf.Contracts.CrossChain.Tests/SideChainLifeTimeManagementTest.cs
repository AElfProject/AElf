using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.ParliamentAuth;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
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

            // release proposal
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;
            var creator = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).Creator;
            Assert.True(creator == Tester.GetCallOwnerAddress());

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value {Value = chainId})).Value;
            Assert.True(chainStatus == (int) SideChainStatus.Active);

            var parentChainId = await Tester.CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetParentChainId), new Empty());
            var parentChain = SInt32Value.Parser.ParseFrom(parentChainId);
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

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                var status = transactionResult.Status;
                Assert.True(status == TransactionResultStatus.Failed);
                Assert.Contains("Invalid chain creation request.", transactionResult.Error);
            }

            {
                var proposalId = await CreateSideChainProposalAsync(10, 0, ByteString.CopyFromUtf8("Test"));
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                var status = transactionResult.Status;
                Assert.True(status == TransactionResultStatus.Failed);
                Assert.Contains("Invalid chain creation request.", transactionResult.Error);
            }
            {
                var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.Empty);
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                var status = transactionResult.Status;
                Assert.True(status == TransactionResultStatus.Failed);
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
            var transactionResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.CreateSideChain),
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", transactionResult.Error);
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

            var balance = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.Equal(10, balance);
        }

        [Fact]
        public async Task CheckLockedBalance_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                new SInt32Value()
                {
                    Value = chainId
                });
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

            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var other = Tester.CreateNewContractTester(ecKeyPair);
            var txResult = await other.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                new SInt32Value()
                {
                    Value = chainId
                });
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

            var lockedToken = SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(
                CrossChainContractAddress, nameof(CrossChainContractContainer.CrossChainContractStub.LockedToken),
                new SInt32Value
                {
                    Value = sideChainId
                })).Value;
            Assert.True(lockedToken == lockedTokenAmount);
        }

        [Fact]
        public async Task GetLockedToken_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var result = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedToken), new SInt32Value
                {
                    Value = sideChainId
                });
            var status = result.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain Not Found.", result.Error);
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

            var result = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedToken), new SInt32Value
                {
                    Value = sideChainId
                });
            var status = result.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Disposed side chain.", result.Error);
        }

        [Fact]
        public async Task GetLockedAddress()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var address = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedAddress), new SInt32Value
                {
                    Value = sideChainId
                }));
            Assert.True(address == Tester.GetCallOwnerAddress());
        }

        [Fact]
        public async Task GetLockedAddress_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var result = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedAddress), new SInt32Value
                {
                    Value = sideChainId
                });
            var status = result.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not existed side chain.", result.Error);
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

            var result = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedToken), new SInt32Value
                {
                    Value = sideChainId
                });
            var status = result.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Disposed side chain.", result.Error);
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
            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Insufficient allowance", transactionResult.Error);

            //with enough token
            await ApproveBalanceAsync(100_000L);
            transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            status = transactionResult.Status;
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

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value {Value = sideChainId})).Value;
            Assert.True(chainStatus == (int) SideChainStatus.Terminated);

            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found or not able to be recharged.", transactionResult.Error);
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
            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found or not able to be recharged.", transactionResult.Error);
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

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value {Value = chainId})).Value;
            Assert.True(chainStatus == (int) SideChainStatus.Terminated);
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

            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var other = Tester.CreateNewContractTester(ecKeyPair);
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await other.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "DisposeSideChain",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = disposalInput.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await other.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to dispose.", transactionResult.Error);
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
            var txResult = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value()
                {
                    Value = chainId
                });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not existed side chain.", txResult.Error);
        }

        #endregion

        [Fact]
        public async Task ChangeOwnerAddress()
        {
            var input = Tester.GetCallOwnerAddress();
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "ChangOwnerAddress",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = input.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var txResult = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ChangOwnerAddress),
                Address.FromString("Test"));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeOwnerAddress_NotAuthorized()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ChangOwnerAddress),
                Address.FromString("Test"));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", txResult.Error);
        }
    }
}