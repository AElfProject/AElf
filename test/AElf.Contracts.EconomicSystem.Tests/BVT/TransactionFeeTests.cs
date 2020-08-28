using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.ContractTestKit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        private const long DefaultFeeAmount = 1_0000_0000;
        private const long NewFeeAmount = 0;
        private const long CreateSchemeAmount = 10_00000000;

        private async Task Vote_SetMethodFee_Test()
        {
            //default fee
            {
                var addOptionFeeAmount = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = nameof(VoteContractStub.AddOption)
                });
                addOptionFeeAmount.MethodName.ShouldBe(string.Empty); //default value is empty
                addOptionFeeAmount.Fees.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                addOptionFeeAmount.Fees.First().BasicFee.ShouldBe(DefaultFeeAmount);
            }

            //set transaction fee
            {
                await Vote_SetMethodFee(nameof(VoteContractStub.AddOption),
                    EconomicSystemTestConstants.NativeTokenSymbol, NewFeeAmount);

                //query result
                var addOptionFeeAmount = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = nameof(VoteContractStub.AddOption)
                });
                addOptionFeeAmount.Fees.Count.ShouldBe(1);
                addOptionFeeAmount.Fees.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                addOptionFeeAmount.Fees.First().BasicFee.ShouldBe(NewFeeAmount);
            }
        }

        [Fact]
        public async Task Vote_Execute_AddOption_Test()
        {
            await Vote_SetMethodFee_Test();

            var registerItem = await RegisterVotingItemAsync(100, 3, true, BootMinerAddress, 1);
            var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = BootMinerAddress
            })).Balance;
            var address = Accounts[10].Address.ToBase58();
            var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
            {
                Option = address,
                VotingItemId = registerItem.VotingItemId
            }));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transactionSize = transactionResult.Transaction.Size();

            var afterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = BootMinerAddress
            })).Balance;
            beforeBalance.ShouldBe(afterBalance + 0 + transactionSize * 0);
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

            var methodFeeController = await EconomicContractStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName =
                nameof(EconomicContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(methodFeeController.ContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });
            await ApproveWithAllMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await EconomicContractStub.GetMethodFeeController.CallAsync(new Empty());
            Assert.True(newMethodFeeController.OwnerAddress == organizationAddress);
        }

        private async Task Profit_SetMethodFee_Test()
        {
            //default fee
            {
                //create scheme
                var createSchemeFeeAmount = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = nameof(ProfitContractStub.CreateScheme)
                });
                createSchemeFeeAmount.MethodName.ShouldBe(string.Empty); //default value is empty
                createSchemeFeeAmount.Fees.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                createSchemeFeeAmount.Fees.First().BasicFee.ShouldBe(CreateSchemeAmount);
                
                //others
                var othersFeeAmount = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = nameof(ProfitContractStub.AddBeneficiaries)
                });
                othersFeeAmount.MethodName.ShouldBe(string.Empty); //default value is empty
                othersFeeAmount.Fees.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                othersFeeAmount.Fees.First().BasicFee.ShouldBe(DefaultFeeAmount);
            }

            //set transaction fee
            {
                await Profit_SetMethodFee(nameof(ProfitContractStub.CreateScheme),
                    EconomicSystemTestConstants.NativeTokenSymbol, NewFeeAmount);

                //query result
                var addOptionFeeAmount = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = nameof(ProfitContractStub.CreateScheme)
                });
                addOptionFeeAmount.Fees.Count.ShouldBe(1);
                addOptionFeeAmount.Fees.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                addOptionFeeAmount.Fees.First().BasicFee.ShouldBe(NewFeeAmount);
            }
        }

        [Fact]
        public async Task Profit_Execute_CreateScheme_Test()
        {
            await Profit_SetMethodFee_Test();

            var tester = Accounts[11].KeyPair;
            var testerAddress = Address.FromPublicKey(tester.PublicKey);
            var creator = GetProfitContractTester(tester);
            var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = testerAddress
            })).Balance;

            var transactionResult = await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = 10
            });

            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transactionSize = transactionResult.Transaction.Size();

            var afterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = testerAddress
            })).Balance;
            beforeBalance.ShouldBe(afterBalance + 0 + transactionSize * 0);
        }

        private async Task Vote_SetMethodFee(string method, string symbol, long feeAmount)
        {
            var gensisOwner = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = gensisOwner,
                ContractMethodName = nameof(VoteContractStub.SetMethodFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new MethodFees
                {
                    MethodName = method,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = symbol,
                            BasicFee = feeAmount
                        }
                    }
                }.ToByteString(),
                ToAddress = VoteContractAddress
            };
            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = createResult.Output;
            await ApproveWithAllMinersAsync(proposalId);

            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task Profit_SetMethodFee(string method, string symbol, long feeAmount)
        {
            var gensisOwner = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = gensisOwner,
                ContractMethodName = nameof(ProfitContractStub.SetMethodFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new MethodFees
                {
                    MethodName = method,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = symbol,
                            BasicFee = feeAmount
                        }
                    }
                }.ToByteString(),
                ToAddress = ProfitContractAddress
            };
            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = createResult.Output;
            await ApproveWithAllMinersAsync(proposalId);

            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<VotingItem> RegisterVotingItemAsync(int lastingDays, int optionsCount, bool isLockToken,
            Address sender,
            int totalSnapshotNumber = int.MaxValue)
        {
            var startTime = TimestampHelper.GetUtcNow();
            var options = Enumerable.Range(0, optionsCount).Select(_ => Accounts[0].Address.ToBase58())
                .ToList();
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = totalSnapshotNumber,
                EndTimestamp = startTime.AddDays(lastingDays),
                StartTimestamp = startTime,
                Options = {options},
                AcceptedCurrency = EconomicSystemTestConstants.NativeTokenSymbol,
                IsLockToken = isLockToken
            };
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            input.Options.Clear();
            var votingItemId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input), HashHelper.ComputeFrom(sender));
            return await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = votingItemId
            });
        }

        private async Task ApproveWithAllMinersAsync(Hash proposalId)
        {
            foreach (var keyPair in InitialCoreDataCenterKeyPairs)
            {
                var parliamentContractStub = GetParliamentContractTester(keyPair);
                var approveResult = await parliamentContractStub.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        protected async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
            string methodName, IMessage input)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = EconomicContractAddress
            };
            
            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;

            return proposalId;
        }
    }
}