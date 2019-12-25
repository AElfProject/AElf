using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.CrossChain.Tests
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
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            Assert.True(creator == DefaultSender);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = chainId});
            Assert.True(chainStatus.Value == (int) SideChainStatus.Active);

            var parentChain = await CrossChainContractStub.GetParentChainId.CallAsync(new Empty());
            Assert.True(parentChain.Equals(new SInt32Value {Value = ChainHelper.ConvertBase58ToChainId("AELF")}));
        }

        [Fact]
        public async Task RequestSideChainCreation_Twice()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount - 1, lockedTokenAmount);
            await CrossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);
            var secondRequestTx =
                await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);
            secondRequestTx.TransactionResult.Error.ShouldContain("Request side chain creation failed.");
        }
        
        [Fact]
        public async Task RequestSideChainCreationSecondTimeAfterRelease()
        {
            await InitializeCrossChainContractAsync();
            {
                long lockedTokenAmount = 10;
                await ApproveBalanceAsync(lockedTokenAmount);
                // Create proposal and approve
                var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
                await ApproveWithMinersAsync(proposalId);
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            }

            {
                long lockedTokenAmount = 10;
                await ApproveBalanceAsync(lockedTokenAmount);
                var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
                proposalId.ShouldNotBeNull();
            }
        }
        
        [Fact]
        public async Task RequestSideChainCreation_Failed()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;

            {
                var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount - 1, lockedTokenAmount);
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Allowance not enough");
            }

            await ApproveBalanceAsync(lockedTokenAmount);

            {
                var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount, lockedTokenAmount);
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Invalid chain creation request.");
            }

            {
                var createProposalInput = CreateSideChainCreationRequest(10, 0);
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Invalid chain creation request.");
            }
        }

        [Fact]
        public async Task CreateSideChainCreation_WithoutRequest()
        {
            await InitializeCrossChainContractAsync();
            var organizationAddress =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalCreation = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.CreateSideChain),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = organizationAddress,
                ToAddress = CrossChainContractAddress,
                Params = new CreateSideChainInput
                {
                    Proposer = DefaultSender,
                    SideChainCreationRequest = new SideChainCreationRequest
                    {
                        IndexingPrice = 1,
                        LockedTokenAmount = 10,
                        SideChainTokenDecimals = 2,
                        IsSideChainTokenBurnable = true,
                        SideChainTokenTotalSupply = 1_000_000_000,
                        SideChainTokenSymbol = "TE",
                        SideChainTokenName = "TEST",
                    }
                }.ToByteString()
            });
            var proposalId = ProposalCreated.Parser.ParseFrom(proposalCreation.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            await ApproveWithMinersAsync(proposalId);
            var proposalRelease = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            proposalRelease.TransactionResult.Error.ShouldContain("Side chain creation failed without proposed data.");
        }
        
        [Fact]
        public async Task ReleaseSideChainCreation_WithoutRequest()
        {
            await InitializeCrossChainContractAsync();
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendWithExceptionAsync(
                    new ReleaseSideChainCreationInput
                        {ProposalId = Hash.Empty});
            releaseTx.TransactionResult.Error.ShouldContain("Release side chain creation failed.");
        }

        [Fact]
        public async Task Create_SideChain_NotAuthorized()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);

            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount);
            var result = await CrossChainContractStub.CreateSideChain.SendWithExceptionAsync(new CreateSideChainInput
                {
                    SideChainCreationRequest = sideChainCreationRequest,
                    Proposer = DefaultSender
                }
            );
            var status = result.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", result.TransactionResult.Error);
        }

        [Fact]
        public async Task CreatSideChain_ByOrganization()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();

            var organization =
                (await ParliamentContractStub.CreateOrganization.SendAsync(new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    }
                })).Output;
            var allowanceResult = await ApproveAndTransferOrganizationBalanceAsync(organization, lockedTokenAmount);
            Assert.True(allowanceResult.Spender == CrossChainContractAddress);
            Assert.True(allowanceResult.Allowance == lockedTokenAmount);

            var createSideChainCreationInput = CreateSideChainCreationRequest(1, lockedTokenAmount);
            var createProposal = await CreateProposalAsync(nameof(CrossChainContractStub.RequestSideChainCreation),
                organization, createSideChainCreationInput);
            await ApproveWithMinersAsync(createProposal);
            var release = await ReleaseProposalAsync(createProposal);
            Assert.True(release.Status == TransactionResultStatus.Mined);

            var createSideChainProposalId = ProposalCreated.Parser
                .ParseFrom(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            await ApproveWithMinersAsync(createSideChainProposalId);

            var releaseInput = new ReleaseSideChainCreationInput {ProposalId = createSideChainProposalId};
            var releaseProposal = await CreateProposalAsync(nameof(CrossChainContractStub.ReleaseSideChainCreation),
                organization, releaseInput);
            await ApproveWithMinersAsync(releaseProposal);
            var releaseRelease = await ReleaseProposalAsync(releaseProposal);
            Assert.True(releaseRelease.Status == TransactionResultStatus.Mined);

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseRelease.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            Assert.True(creator == organization);

            var chainLockedBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {Symbol = "ELF", Owner = CrossChainContractAddress});
            Assert.True(chainLockedBalance.Balance == lockedTokenAmount);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = chainId});
            Assert.True(chainStatus.Value == (int) SideChainStatus.Active);

            var parentChain = await CrossChainContractStub.GetParentChainId.CallAsync(new Empty());
            Assert.True(parentChain.Equals(new SInt32Value {Value = ChainHelper.ConvertBase58ToChainId("AELF")}));
        }

        [Fact]
        public async Task CheckLockedBalance()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;
            var balance =
                (await CrossChainContractStub.GetSideChainBalance.CallAsync(new SInt32Value {Value = chainId}))
                .Value;
            Assert.Equal(10, balance);
        }

        [Fact]
        public async Task CheckLockedBalance_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult =
                (await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new SInt32Value
                    {Value = chainId}))
                .TransactionResult;
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", txResult.Error);
        }
        
        [Fact]
        public async Task GetLockedToken()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var lockedToken = await CrossChainContractStub.GetSideChainBalance.CallAsync(new SInt32Value
            {
                Value = sideChainId
            });
            Assert.True(lockedToken.Value == lockedTokenAmount);
        }

        [Fact]
        public async Task GetLockedToken_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new SInt32Value
            {
                Value = sideChainId
            });

            Assert.True(res.TransactionResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task GetSideChainBalance_DisposedChain()
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

            var res = await CrossChainContractStub.GetSideChainBalance.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var actual = SInt64Value.Parser.ParseFrom(res.TransactionResult.ReturnValue);
            Assert.True(0 == actual.Value);
        }

        [Fact]
        public async Task GetSideChainCreator()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var address = await CrossChainContractStub.GetSideChainCreator.CallAsync(new SInt32Value
            {
                Value = sideChainId
            });

            Assert.True(address == DefaultSender);
        }

        [Fact]
        public async Task GetGetSideChainCreator_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new SInt32Value
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

            var res = await CrossChainContractStub.GetSideChainCreator.SendAsync(new SInt32Value
            {
                Value = sideChainId
            });

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var actual = Address.Parser.ParseFrom(res.TransactionResult.ReturnValue);
            Assert.Equal(DefaultSender, actual);
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
            var res = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);
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
            var res = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);

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
            var res = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);

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
            var chainId = await InitAndCreateSideChainAsync(withException: true);

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
        public async Task Disposal_SideChain_NotFound()
        {
            await InitializeCrossChainContractAsync();
            var chainId = ChainHelper.GetChainId(1);
            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalWithExceptionAsync(proposalId);
            var status = transactionResult.Status;

            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", transactionResult.Error);
        }

        [Fact]
        public async Task Disposal_SideChain_WrongStatus()
        {
            long lockedTokenAmount = 10;
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
            var transactionResult2 = await ReleaseProposalWithExceptionAsync(proposalId2);
            var status2 = transactionResult2.Status;
            Assert.True(status2 == TransactionResultStatus.Failed);
            Assert.Contains("Incorrect chain status.", transactionResult2.Error);
        }

        [Fact]
        public async Task GetChainStatus_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.GetChainStatus.SendWithExceptionAsync(new SInt32Value
                {Value = chainId});
            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        #endregion

        [Fact]
        public async Task ChangeOwnerAddress()
        {
            var organizationAddress =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
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
            var res = (await CrossChainContractStub.ChangOwnerAddress.SendWithExceptionAsync(DefaultSender))
                .TransactionResult;

            var status = res.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", res.Error);
        }
    }
}