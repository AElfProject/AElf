using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Parliament.CreateOrganizationInput;

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

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value {Value = chainId});
            Assert.True(chainStatus.Status == SideChainStatus.Active);

            var parentChain = await CrossChainContractStub.GetParentChainId.CallAsync(new Empty());
            Assert.True(parentChain.Equals(new Int32Value {Value = ChainHelper.ConvertBase58ToChainId("AELF")}));
        }

        [Fact]
        public async Task RequestSideChainCreation_Twice()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var utcNow = TimestampHelper.GetUtcNow();
            BlockTimeProvider.SetBlockTime(utcNow);
            var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount - 1, lockedTokenAmount,
                GetValidResourceAmount(), new[]
                {
                    new SideChainTokenInitialIssue
                    {
                        Address = DefaultSender,
                        Amount = 100
                    }
                });
            await CrossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);

            BlockTimeProvider.SetBlockTime(utcNow.AddSeconds(86399));
            var secondRequestTx =
                await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);
            secondRequestTx.TransactionResult.Error.ShouldContain("Request side chain creation failed.");

            BlockTimeProvider.SetBlockTime(utcNow.AddSeconds(86400));
            var thirdRequestTx = await CrossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);
            thirdRequestTx.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
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
                var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount - 1, lockedTokenAmount,
                    GetValidResourceAmount(), new[]
                    {
                        new SideChainTokenInitialIssue
                        {
                            Address = DefaultSender,
                            Amount = 100
                        }
                    });
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Allowance not enough");
            }

            await ApproveBalanceAsync(lockedTokenAmount);

            {
                var createProposalInput = CreateSideChainCreationRequest(lockedTokenAmount, lockedTokenAmount,
                    GetValidResourceAmount(), new[]
                    {
                        new SideChainTokenInitialIssue
                        {
                            Address = DefaultSender,
                            Amount = 100
                        }
                    }, true);
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Invalid chain creation request.");
            }

            {
                var createProposalInput = CreateSideChainCreationRequest(10, 0, GetValidResourceAmount(),
                    new[]
                    {
                        new SideChainTokenInitialIssue
                        {
                            Address = DefaultSender,
                            Amount = 100
                        }
                    }, true);
                var requestSideChainCreation =
                    await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(createProposalInput);

                requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                requestSideChainCreation.TransactionResult.Error.ShouldContain("Invalid chain creation request.");
            }

            {
                foreach (var t in ResourceTokenSymbolList)
                {
                    // invalid resource token
                    var resourceAmount = GetValidResourceAmount();
                    resourceAmount.Remove(t);
                    var createProposalInput = CreateSideChainCreationRequest(1, lockedTokenAmount,
                        resourceAmount, new[]
                        {
                            new SideChainTokenInitialIssue
                            {
                                Address = DefaultSender,
                                Amount = 100
                            }
                        }, true);
                    var requestSideChainCreation =
                        await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(
                            createProposalInput);

                    requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                    requestSideChainCreation.TransactionResult.Error.ShouldContain(
                        "Invalid side chain resource token request.");
                }

                foreach (var t in ResourceTokenSymbolList)
                {
                    // invalid resource token
                    var resourceAmount = GetValidResourceAmount();
                    resourceAmount[t] = 0;
                    var createProposalInput = CreateSideChainCreationRequest(1, lockedTokenAmount,
                        resourceAmount, new[]
                        {
                            new SideChainTokenInitialIssue
                            {
                                Address = DefaultSender,
                                Amount = 100
                            }
                        }, true);
                    var requestSideChainCreation =
                        await CrossChainContractStub.RequestSideChainCreation.SendWithExceptionAsync(
                            createProposalInput);

                    requestSideChainCreation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                    requestSideChainCreation.TransactionResult.Error.ShouldContain(
                        "Invalid side chain resource token request.");
                }
            }
        }

        [Fact]
        public async Task CreateSideChainCreation_WithoutRequest()
        {
            await InitializeCrossChainContractAsync();
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
            var proposalCreation = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.CreateSideChain),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = oldOrganizationAddress,
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

            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount,
                GetValidResourceAmount(), new[]
                {
                    new SideChainTokenInitialIssue
                    {
                        Address = DefaultSender,
                        Amount = 100
                    }
                });
            var result = await CrossChainContractStub.CreateSideChain.SendWithExceptionAsync(new CreateSideChainInput
                {
                    SideChainCreationRequest = sideChainCreationRequest,
                    Proposer = DefaultSender
                }
            );
            var status = result.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Unauthorized behavior.", result.TransactionResult.Error);
        }

        [Fact]
        public async Task CreatSideChain_ByOrganization()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();

            var organizationAddress =
                (await ParliamentContractStub.CreateOrganization.SendAsync(new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    },
                    ParliamentMemberProposingAllowed = true
                })).Output;
            var allowanceResult =
                await ApproveAndTransferOrganizationBalanceAsync(organizationAddress, lockedTokenAmount);
            Assert.True(allowanceResult.Spender == CrossChainContractAddress);
            Assert.True(allowanceResult.Allowance == lockedTokenAmount);

            var createSideChainCreationInput = CreateSideChainCreationRequest(1, lockedTokenAmount,
                GetValidResourceAmount(), new[]
                {
                    new SideChainTokenInitialIssue
                    {
                        Address = DefaultSender,
                        Amount = 100
                    }
                });
            var createProposal = await CreateParliamentProposalAsync(
                nameof(CrossChainContractStub.RequestSideChainCreation),
                organizationAddress, createSideChainCreationInput);
            await ApproveWithMinersAsync(createProposal);
            var release = await ReleaseProposalAsync(createProposal);
            Assert.True(release.Status == TransactionResultStatus.Mined);

            var createSideChainProposalId = ProposalCreated.Parser
                .ParseFrom(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            await ApproveWithMinersAsync(createSideChainProposalId);

            var releaseInput = new ReleaseSideChainCreationInput {ProposalId = createSideChainProposalId};
            var releaseProposal = await CreateParliamentProposalAsync(
                nameof(CrossChainContractStub.ReleaseSideChainCreation),
                organizationAddress, releaseInput);
            await ApproveWithMinersAsync(releaseProposal);
            var releaseRelease = await ReleaseProposalAsync(releaseProposal);
            Assert.True(releaseRelease.Status == TransactionResultStatus.Mined);

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseRelease.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            Assert.True(creator == organizationAddress);

            var chainLockedBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {Symbol = "ELF", Owner = CrossChainContractAddress});
            Assert.True(chainLockedBalance.Balance == lockedTokenAmount);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value {Value = chainId});
            Assert.True(chainStatus.Status == SideChainStatus.Active);

            var parentChain = await CrossChainContractStub.GetParentChainId.CallAsync(new Empty());
            Assert.True(parentChain.Equals(new Int32Value {Value = ChainHelper.ConvertBase58ToChainId("AELF")}));
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
                (await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value {Value = chainId}))
                .Value;
            Assert.Equal(10, balance);
        }

        [Fact]
        public async Task CheckLockedBalance_NotExist()
        {
            var chainId = ChainHelper.GetChainId(1);
            var txResult =
                (await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new Int32Value
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

            var lockedToken = await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            Assert.True(lockedToken.Value == lockedTokenAmount);
        }

        [Fact]
        public async Task GetLockedToken_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new Int32Value
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

            var proposalId = await DisposeSideChainProposalAsync(new Int32Value
            {
                Value = sideChainId
            });
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var res = await CrossChainContractStub.GetSideChainBalance.SendAsync(new Int32Value
            {
                Value = sideChainId
            });

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var actual = Int64Value.Parser.ParseFrom(res.TransactionResult.ReturnValue);
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

            var address = await CrossChainContractStub.GetSideChainCreator.CallAsync(new Int32Value
            {
                Value = sideChainId
            });

            Assert.True(address == DefaultSender);
        }

        [Fact]
        public async Task GetGetSideChainCreator_NotExist()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var res = await CrossChainContractStub.GetSideChainBalance.SendWithExceptionAsync(new Int32Value
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

            var proposalId = await DisposeSideChainProposalAsync(new Int32Value
            {
                Value = sideChainId
            });
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var res = await CrossChainContractStub.GetSideChainCreator.SendAsync(new Int32Value
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

            var proposalId = await DisposeSideChainProposalAsync(new Int32Value {Value = sideChainId});
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });

            Assert.True(chainStatus.Status == SideChainStatus.Terminated);
            var res = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);

            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found or incorrect side chain status.", res.TransactionResult.Error);
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
            Assert.Contains("Side chain not found or incorrect side chain status.", res.TransactionResult.Error);
        }

        [Fact]
        public async Task DisposeSideChain()
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

            var proposalId = await DisposeSideChainProposalAsync(new Int32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);

            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value {Value = chainId});
            Assert.True(chainStatus.Status == SideChainStatus.Terminated);

            var balanceAfterDisposal = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = CrossChainContractAddress,
                Symbol = "ELF"
            });
            Assert.True(balanceAfterDisposal.Balance == 0);

            // try to adjust indexing fee after disposal
            var indexingFeeAdjustingTx = await CrossChainContractStub.AdjustIndexingFeePrice.SendWithExceptionAsync(
                new AdjustIndexingFeeInput
                {
                    SideChainId = chainId,
                    IndexingFee = lockedTokenAmount - 1
                });
            indexingFeeAdjustingTx.TransactionResult.Error.ShouldContain(
                "Side chain not found or incorrect side chain status.");
        }

        [Fact]
        public async Task DisposeSideChain_NotFound()
        {
            await InitializeCrossChainContractAsync();
            var chainId = ChainHelper.GetChainId(1);
            var proposalId = await DisposeSideChainProposalAsync(new Int32Value
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
        public async Task DisposeSideChain_Insufficient_Balance()
        {
            long lockedTokenAmount = 1;
            await ApproveBalanceAsync(lockedTokenAmount);
            var chainId = await InitAndCreateSideChainAsync();
            var proposalId1 = await DisposeSideChainProposalAsync(new Int32Value
            {
                Value = chainId
            });
            await ApproveWithMinersAsync(proposalId1);
            var transactionResult1 = await ReleaseProposalAsync(proposalId1);
            var status1 = transactionResult1.Status;
            Assert.True(status1 == TransactionResultStatus.Mined);

            var proposalId2 = await DisposeSideChainProposalAsync(new Int32Value
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
            var res = await CrossChainContractStub.GetChainStatus.SendWithExceptionAsync(new Int32Value
                {Value = chainId});
            var status = res.TransactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Side chain not found.", res.TransactionResult.Error);
        }

        #endregion

        [Fact]
        public async Task ChangeIndexingController()
        {
            await InitializeCrossChainContractAsync();
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty())).OwnerAddress;

            var newOrganizationAddress = (await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 3333,
                        MaximalRejectionThreshold = 3333,
                        MinimalApprovalThreshold = 3333,
                        MinimalVoteThreshold = 3333
                    },
                    ParliamentMemberProposingAllowed = true
                })).Output;
            var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.ChangeCrossChainIndexingController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new AuthorityInfo
                {
                    ContractAddress = ParliamentContractAddress, OwnerAddress = newOrganizationAddress
                }.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = oldOrganizationAddress
            });

            var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = (await ParliamentContractStub.Release.SendAsync(proposalId)).TransactionResult;
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var crossChainIndexingController =
                await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty());
            crossChainIndexingController.ContractAddress.ShouldBe(ParliamentContractAddress);
            crossChainIndexingController.OwnerAddress.ShouldBe(newOrganizationAddress);
        }

        [Fact]
        public async Task ChangeIndexingController_InvalidOwnerAddress()
        {
            await InitializeCrossChainContractAsync();
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty())).OwnerAddress;
            var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.ChangeCrossChainIndexingController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new AuthorityInfo
                {
                    ContractAddress = ParliamentContractAddress, OwnerAddress = DefaultSender
                }.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = oldOrganizationAddress
            });

            var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = (await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId))
                .TransactionResult;
            releaseResult.Error.ShouldContain("Invalid authority input.");
        }


        [Fact]
        public async Task ChangeIndexingController_NotAuthorized()
        {
            await InitializeCrossChainContractAsync();
            var newOrganizationAddress = (await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 3333,
                        MaximalRejectionThreshold = 3333,
                        MinimalApprovalThreshold = 3333,
                        MinimalVoteThreshold = 3333
                    }
                })).Output;

            var res =
                (await CrossChainContractStub.ChangeCrossChainIndexingController.SendWithExceptionAsync(
                    new AuthorityInfo
                    {
                        ContractAddress = ParliamentContractAddress, OwnerAddress = newOrganizationAddress
                    })).TransactionResult;

            var status = res.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Unauthorized behavior.", res.Error);
        }
        
        [Fact]
        public async Task ChangeIndexingController_InvalidOrganization()
        {
            await InitializeCrossChainContractAsync();
            var newOrganizationCreationInput = new Association.CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {DefaultSender}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MaximalRejectionThreshold = 0,
                    MaximalAbstentionThreshold = 0
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {DefaultSender}
                }
            };
            var newOrganizationAddress =
                (await AssociationContractStub.CreateOrganization.SendAsync(newOrganizationCreationInput)).Output;
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty())).OwnerAddress;
            var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.ChangeCrossChainIndexingController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new AuthorityInfo
                {
                    ContractAddress = AssociationContractAddress, OwnerAddress = newOrganizationAddress
                }.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = oldOrganizationAddress
            });

            var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = (await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId))
                .TransactionResult;
            releaseResult.Error.ShouldContain("Invalid authority input.");
        }

        [Fact]
        public async Task ChangeSideChainLifeTimeController()
        {
            await InitializeCrossChainContractAsync();
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;

            var newOrganizationAddress = (await AssociationContractStub.CreateOrganization.SendAsync(
                new Association.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 0,
                        MaximalRejectionThreshold = 0,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {DefaultSender}
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = {DefaultSender}
                    }
                })).Output;

            var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.ChangeSideChainLifetimeController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new AuthorityInfo
                {
                    ContractAddress = AssociationContractAddress, OwnerAddress = newOrganizationAddress
                }.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = oldOrganizationAddress
            });

            var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = (await ParliamentContractStub.Release.SendAsync(proposalId)).TransactionResult;
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var sideChainLifetimeController =
                await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty());
            sideChainLifetimeController.ContractAddress.ShouldBe(AssociationContractAddress);
            sideChainLifetimeController.OwnerAddress.ShouldBe(newOrganizationAddress);
        }

        [Fact]
        public async Task ChangeSideChainLifeTimeController_Failed()
        {
            await InitializeCrossChainContractAsync();
            var oldOrganizationAddress =
                (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;

            {
                // invalid new owner address
                var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
                {
                    ContractMethodName = nameof(CrossChainContractStub.ChangeSideChainLifetimeController),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = new AuthorityInfo
                    {
                        ContractAddress = ParliamentContractAddress, OwnerAddress = DefaultSender
                    }.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = oldOrganizationAddress
                });

                var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
                await ApproveWithMinersAsync(proposalId);
                var releaseResult = (await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId))
                    .TransactionResult;
                releaseResult.Error.ShouldContain("Invalid authority input.");
            }

            {
                // wrong authorization contract address
                var proposalRes = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
                {
                    ContractMethodName = nameof(CrossChainContractStub.ChangeSideChainLifetimeController),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = new AuthorityInfo
                    {
                        ContractAddress = AssociationContractAddress, OwnerAddress = oldOrganizationAddress
                    }.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = oldOrganizationAddress
                });

                var proposalId = Hash.Parser.ParseFrom(proposalRes.TransactionResult.ReturnValue);
                await ApproveWithMinersAsync(proposalId);
                var releaseResult = (await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId))
                    .TransactionResult;
                releaseResult.Error.ShouldContain("Invalid authority input.");
            }

            {
                // no proposal created
                var newOrganizationAddress = (await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MaximalAbstentionThreshold = 3333,
                            MaximalRejectionThreshold = 3333,
                            MinimalApprovalThreshold = 3333,
                            MinimalVoteThreshold = 3333
                        }
                    })).Output;

                var res =
                    (await CrossChainContractStub.ChangeSideChainLifetimeController.SendWithExceptionAsync(
                        new AuthorityInfo
                        {
                            ContractAddress = ParliamentContractAddress, OwnerAddress = newOrganizationAddress
                        })).TransactionResult;

                var status = res.Status;
                Assert.True(status == TransactionResultStatus.Failed);
                Assert.Contains("Unauthorized behavior.", res.Error);
            }
        }

        [Fact]
        public async Task AdjustCrossChainIndexingFeePriceTest()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var organizationAddress = OrganizationCreated.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(OrganizationCreated)))
                    .NonIndexed).OrganizationAddress;
            organizationAddress.ShouldNotBeNull();

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var sideChainId = sideChainCreatedEvent.ChainId;
            {
                var newIndexingFeePrice = -1;
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    organizationAddress, CrossChainContractAddress, new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                var parliamentOrganizationAddress =
                    (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
                var approveProposalId = await CreateParliamentProposalAsync(nameof(AssociationContractStub.Approve),
                    parliamentOrganizationAddress, indexingFeeAdjustProposalId, AssociationContractAddress);
                await ApproveWithMinersAsync(approveProposalId);
                await ParliamentContractStub.Release.SendAsync(approveProposalId);
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);
                var txResult =
                    (await AssociationContractStub.Release.SendWithExceptionAsync(indexingFeeAdjustProposalId))
                    .TransactionResult;
                txResult.Error.ShouldContain("Invalid side chain fee price.");
            }

            {
                var newIndexingFeePrice = 2;
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    organizationAddress, CrossChainContractAddress, new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                var parliamentOrganizationAddress =
                    (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
                var approveProposalId = await CreateParliamentProposalAsync(nameof(AssociationContractStub.Approve),
                    parliamentOrganizationAddress, indexingFeeAdjustProposalId, AssociationContractAddress);
                await ApproveWithMinersAsync(approveProposalId);
                await ParliamentContractStub.Release.SendAsync(approveProposalId);
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);
                await AssociationContractStub.Release.SendAsync(indexingFeeAdjustProposalId);

                var indexingFeePriceCheck =
                    await CrossChainContractStub.GetSideChainIndexingFeePrice.SendAsync(new Int32Value()
                        {Value = sideChainId});
                indexingFeePriceCheck.Output.Value.ShouldBe(newIndexingFeePrice);
            }
        }

        [Fact]
        public async Task AdjustCrossChainIndexingFeePriceTest_InsufficientBalance_Dispose()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var organizationAddress = OrganizationCreated.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(OrganizationCreated)))
                    .NonIndexed).OrganizationAddress;
            organizationAddress.ShouldNotBeNull();

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var sideChainId = sideChainCreatedEvent.ChainId;
            {
                var newIndexingFeePrice = 10;
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    organizationAddress, CrossChainContractAddress, new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                var parliamentOrganizationAddress =
                    (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
                var approveProposalId = await CreateParliamentProposalAsync(nameof(AssociationContractStub.Approve),
                    parliamentOrganizationAddress, indexingFeeAdjustProposalId, AssociationContractAddress);
                await ApproveWithMinersAsync(approveProposalId);
                await ParliamentContractStub.Release.SendAsync(approveProposalId);
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);
                await AssociationContractStub.Release.SendAsync(indexingFeeAdjustProposalId);

                var indexingFeePriceCheck =
                    await CrossChainContractStub.GetSideChainIndexingFeePrice.SendAsync(new Int32Value()
                        {Value = sideChainId});
                indexingFeePriceCheck.Output.Value.ShouldBe(newIndexingFeePrice);
            }

            {
                var newIndexingFeePrice = 11;
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    organizationAddress, CrossChainContractAddress, new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                var parliamentOrganizationAddress =
                    (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
                var approveProposalId = await CreateParliamentProposalAsync(nameof(AssociationContractStub.Approve),
                    parliamentOrganizationAddress, indexingFeeAdjustProposalId, AssociationContractAddress);
                await ApproveWithMinersAsync(approveProposalId);
                await ParliamentContractStub.Release.SendAsync(approveProposalId);
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);
                await AssociationContractStub.Release.SendAsync(indexingFeeAdjustProposalId);

                var indexingFeePriceCheck =
                    await CrossChainContractStub.GetSideChainIndexingFeePrice.SendAsync(new Int32Value()
                        {Value = sideChainId});
                indexingFeePriceCheck.Output.Value.ShouldBe(newIndexingFeePrice);

                var sideChainStatus =
                    await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value {Value = sideChainId});
                sideChainStatus.Status.ShouldBe(SideChainStatus.InsufficientBalance);

                var disposalProposalId = await DisposeSideChainProposalAsync(new Int32Value
                {
                    Value = sideChainId
                });
                await ApproveWithMinersAsync(disposalProposalId);
                var transactionResult = await ReleaseProposalAsync(disposalProposalId);
                var status = transactionResult.Status;
                status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task AdjustCrossChainIndexingFeePriceTest_AfterSideChainLifetimeControllerChanged()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);
            var sideChainCreationReleaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var organizationAddress = OrganizationCreated.Parser
                .ParseFrom(sideChainCreationReleaseTx.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(OrganizationCreated)))
                    .NonIndexed).OrganizationAddress;
            organizationAddress.ShouldNotBeNull();

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(sideChainCreationReleaseTx.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var sideChainId = sideChainCreatedEvent.ChainId;

            // change side chain lifetime controller
            var newSideChainLifetimeControllerAddress = (await AssociationContractStub.CreateOrganization.SendAsync(
                new Association.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 0,
                        MaximalRejectionThreshold = 0,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {AnotherSenderAddress}
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = {AnotherSenderAddress}
                    }
                })).Output;

            var oldSideChainLifetimeController =
                (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
            var sideChainLifetimeControllerChangeProposalId = await CreateParliamentProposalAsync(
                nameof(CrossChainContractStub.ChangeSideChainLifetimeController), oldSideChainLifetimeController,
                new AuthorityInfo
                {
                    ContractAddress = AssociationContractAddress, OwnerAddress = newSideChainLifetimeControllerAddress
                });

            await ApproveWithMinersAsync(sideChainLifetimeControllerChangeProposalId);
            await ParliamentContractStub.Release.SendAsync(sideChainLifetimeControllerChangeProposalId);

            var newIndexingFeePrice = 2;

            {
                // use old controller to create proposal
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    organizationAddress, CrossChainContractAddress, new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                var approveProposalId = await CreateParliamentProposalAsync(nameof(AssociationContractStub.Approve),
                    oldSideChainLifetimeController, indexingFeeAdjustProposalId, AssociationContractAddress);
                await ApproveWithMinersAsync(approveProposalId);
                await ParliamentContractStub.Release.SendAsync(approveProposalId);
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);
                var releaseTx =
                    await AssociationContractStub.Release.SendWithExceptionAsync(indexingFeeAdjustProposalId);
                releaseTx.TransactionResult.Error.ShouldContain("No permission.");
            }

            {
                var indexingFeeController =
                    await CrossChainContractStub.GetSideChainIndexingFeeController.CallAsync(new Int32Value
                    {
                        Value = sideChainId
                    });
                await AssociationContractStub.CreateOrganization.SendAsync(
                    AElf.Contracts.Association.CreateOrganizationInput.Parser.ParseFrom(indexingFeeController
                        .OrganizationCreationInputBytes));


                // side chain fee adjustment proposal
                var indexingFeeAdjustProposalId = await CreateAssociationProposalAsync(
                    nameof(CrossChainContractStub.AdjustIndexingFeePrice),
                    indexingFeeController.AuthorityInfo.OwnerAddress, CrossChainContractAddress,
                    new AdjustIndexingFeeInput
                    {
                        IndexingFee = newIndexingFeePrice,
                        SideChainId = sideChainId
                    });

                // side chain creator creates proposal
                var anotherSenderAssociationContractStub = GetAssociationContractStub(AnotherKeyPair);
                var sideChainLifetimeControllerProposalId = await CreateAssociationProposalAsync(
                    nameof(AssociationContractStub.Approve), newSideChainLifetimeControllerAddress,
                    AssociationContractAddress, indexingFeeAdjustProposalId, anotherSenderAssociationContractStub);

                // side chain lifetime controller approve
                await anotherSenderAssociationContractStub.Approve.SendAsync(sideChainLifetimeControllerProposalId);
                // release to approve
                await anotherSenderAssociationContractStub.Release.SendAsync(sideChainLifetimeControllerProposalId);

                // side chain creator approve
                await AssociationContractStub.Approve.SendAsync(indexingFeeAdjustProposalId);

                // release
                await AssociationContractStub.Release.SendAsync(indexingFeeAdjustProposalId);

                var indexingFeePriceCheck =
                    await CrossChainContractStub.GetSideChainIndexingFeePrice.SendAsync(new Int32Value()
                        {Value = sideChainId});
                indexingFeePriceCheck.Output.Value.ShouldBe(newIndexingFeePrice);
            }
        }

        [Fact]
        public async Task ChangeMethodFeeController_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);

            var methodFeeController = await CrossChainContractStub.GetMethodFeeController.CallAsync(new Empty());
            const string proposalCreationMethodName =
                nameof(CrossChainContractStub.ChangeMethodFeeController);
            var proposalId = await CreateParliamentProposalAsync(proposalCreationMethodName,
                methodFeeController.OwnerAddress,
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await CrossChainContractStub.GetMethodFeeController.CallAsync(new Empty());
            Assert.True(newMethodFeeController.OwnerAddress == organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);
            var methodFeeController = await CrossChainContractStub.GetMethodFeeController.CallAsync(new Empty());
            var result = await CrossChainContractStub.ChangeMethodFeeController.SendWithExceptionAsync(
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeCrossChainMethodFee_Test()
        {
            await InitializeCrossChainContractAsync();
            var organizationAddress =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var methodName = nameof(CrossChainContractStub.Recharge);
            var proposal = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ToAddress = CrossChainContractAddress,
                ContractMethodName = nameof(CrossChainContractStub.SetMethodFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = organizationAddress,
                Params = new MethodFees
                {
                    MethodName = methodName,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "ELF",
                            BasicFee = 5000_0000L
                        }
                    }
                }.ToByteString()
            });
            var proposalId = proposal.Output;
            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);

            var methodFee = await CrossChainContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            });
            methodFee.MethodName.ShouldBe(methodName);
            methodFee.Fees.First().ShouldBe(new MethodFee
            {
                Symbol = "ELF",
                BasicFee = 5000_0000L
            });
        }

        [Fact]
        public async Task GetSideChainIdAndHeight_Test()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var sideChainIdAndHeight = await CrossChainContractStub.GetSideChainIdAndHeight.CallAsync(new Empty());
            sideChainIdAndHeight.IdHeightDict.Count.ShouldBe(1);
            sideChainIdAndHeight.IdHeightDict.Keys.First().ShouldBe(sideChainId);
        }

        [Fact]
        public async Task GetAllChainsIdAndHeight_Test()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var sideChainIdAndHeight = await CrossChainContractStub.GetAllChainsIdAndHeight.CallAsync(new Empty());
            sideChainIdAndHeight.IdHeightDict.Count.ShouldBe(2);
            var chainIds = sideChainIdAndHeight.IdHeightDict.Keys;
            chainIds.ShouldContain(parentChainId);
            chainIds.ShouldContain(sideChainId);
        }

        [Fact]
        public async Task GetSideChainIndexingInformationList_Test()
        {
            var parentChainId = 123;
            var lockedTokenAmount = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedTokenAmount);

            var sideChainIndexingInformationList =
                await CrossChainContractStub.GetSideChainIndexingInformationList.CallAsync(new Empty());
            sideChainIndexingInformationList.IndexingInformationList.Count.ShouldBe(1);
            var indexInfo = sideChainIndexingInformationList.IndexingInformationList.First();
            indexInfo.ChainId.ShouldBe(sideChainId);
            indexInfo.IndexedHeight.ShouldBe(0);
        }

        [Fact]
        public async Task GetSideChainInitializationDataTest()
        {
            {
                long lockedTokenAmount = 10;
                await ApproveBalanceAsync(lockedTokenAmount * 2);
                // Create proposal and approve
                var parentChainId = ChainHelper.ConvertBase58ToChainId("AELF");
                var txResult =
                    await CreateSideChainByDefaultSenderAsync(true, 0, parentChainId, lockedTokenAmount, 1, true);
                var sideChainCreatedEvent = SideChainCreatedEvent.Parser.ParseFrom(txResult.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent))).NonIndexed);
                
                var sideChainId = sideChainCreatedEvent.ChainId;
                var chainInitializationData =
                    await CrossChainContractStub.GetChainInitializationData.CallAsync(new Int32Value
                    {
                        Value = sideChainId
                    });
                chainInitializationData.ShouldNotBeNull();
                chainInitializationData.Creator.ShouldBe(DefaultSender);
                chainInitializationData.ChainId.ShouldBe(sideChainId);
                chainInitializationData.CreationHeightOnParentChain.ShouldBe(txResult.BlockNumber);
                chainInitializationData.ChainCreatorPrivilegePreserved.ShouldBeTrue();
                chainInitializationData.ParentChainTokenContractAddress.ShouldBe(TokenContractAddress);
                TokenInfo.Parser.ParseFrom(chainInitializationData.NativeTokenInfoData).Symbol.ShouldBe("ELF");
                TokenInfo.Parser.ParseFrom(chainInitializationData.ChainPrimaryTokenInfo.ChainPrimaryTokenData).Symbol
                    .ShouldBe("TE");
                chainInitializationData.ResourceTokenInfo.ShouldNotBeNull();
            }

            {
                long lockedTokenAmount = 10;
                await ApproveBalanceAsync(lockedTokenAmount * 2);
                // Create proposal and approve
                var parentChainId = ChainHelper.ConvertBase58ToChainId("AELF");
                var txResult =
                    await CreateSideChainByDefaultSenderAsync(false, 0, parentChainId, lockedTokenAmount, 1, false);
                var sideChainCreatedEvent = SideChainCreatedEvent.Parser.ParseFrom(txResult.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent))).NonIndexed);
                var sideChainId = sideChainCreatedEvent.ChainId;
                var chainInitializationData =
                    await CrossChainContractStub.GetChainInitializationData.CallAsync(new Int32Value
                    {
                        Value = sideChainId
                    });
                chainInitializationData.ShouldNotBeNull();
                chainInitializationData.Creator.ShouldBe(DefaultSender);
                chainInitializationData.ChainId.ShouldBe(sideChainId);
                chainInitializationData.CreationHeightOnParentChain.ShouldBe(txResult.BlockNumber);
                chainInitializationData.ChainCreatorPrivilegePreserved.ShouldBeFalse();
                chainInitializationData.ParentChainTokenContractAddress.ShouldBe(TokenContractAddress);
                TokenInfo.Parser.ParseFrom(chainInitializationData.NativeTokenInfoData).Symbol.ShouldBe("ELF");
                chainInitializationData.ChainPrimaryTokenInfo.ShouldBeNull();
                chainInitializationData.ResourceTokenInfo.ShouldNotBeNull();
            }
        }
    }
}