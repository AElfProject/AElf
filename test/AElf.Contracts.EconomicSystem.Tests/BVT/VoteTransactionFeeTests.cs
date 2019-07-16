using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Vote;
using AElf.Kernel;
using AElf.Sdk.CSharp;
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
        private const long NewFeeAmount = 1_5000_0000;

        [Fact]
        public async Task SetMethodFee_Test()
        {
            //default fee
            {
                var addOptionFeeAmount = await VoteContractStub.GetMethodFee.CallAsync(new MethodName
                {
                    Name = nameof(VoteContractStub.AddOption)
                });
                addOptionFeeAmount.Method.ShouldBe(string.Empty); //default value is empty
                addOptionFeeAmount.Amounts.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                addOptionFeeAmount.Amounts.First().Amount.ShouldBe(DefaultFeeAmount);
            }

            //set transaction fee
            {
                await SetMethodFee(nameof(VoteContractStub.AddOption), EconomicSystemTestConstants.NativeTokenSymbol, NewFeeAmount);

                //query result
                var addOptionFeeAmount = await VoteContractStub.GetMethodFee.CallAsync(new MethodName
                {
                    Name = nameof(VoteContractStub.AddOption)
                });
                addOptionFeeAmount.Amounts.Count.ShouldBe(1);
                addOptionFeeAmount.Amounts.First().Symbol.ShouldBe(EconomicSystemTestConstants.NativeTokenSymbol);
                addOptionFeeAmount.Amounts.First().Amount.ShouldBe(NewFeeAmount);
            }
        }

        [Fact]
        public async Task Execute_AddOption()
        {
            await SetMethodFee_Test();

            var registerItem = await RegisterVotingItemAsync(100, 3, true, BootMinerAddress, 1);
            var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = BootMinerAddress
            })).Balance;
            var address = Address.Generate().GetFormatted();
            var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
            {
                Option = address,
                VotingItemId = registerItem.VotingItemId
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Owner = BootMinerAddress
            })).Balance;
            beforeBalance.ShouldBe(afterBalance + NewFeeAmount);
        }

        private async Task SetMethodFee(string method, string symbol, long feeAmount)
        {
            var gensisOwner = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = gensisOwner,
                ContractMethodName = nameof(VoteContractStub.SetMethodFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new TokenAmounts
                {
                    Method = method,
                    Amounts =
                    {
                        new TokenAmount
                        {
                            Symbol = symbol,
                            Amount = feeAmount
                        }
                    }
                }.ToByteString(),
                ToAddress = VoteContractAddress
            };
            var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
           createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalHash = Hash.FromMessage(proposal);
            var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
            {
                ProposalId = proposalHash,
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var releaseResult = await ParliamentAuthContractStub.Release.SendAsync(proposalHash);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        private async Task<VotingItem> RegisterVotingItemAsync(int lastingDays, int optionsCount, bool isLockToken, Address sender,
            int totalSnapshotNumber = int.MaxValue)
        {
            var startTime = TimestampHelper.GetUtcNow();
            var options = Enumerable.Range(0, optionsCount).Select(_ => Address.Generate().GetFormatted()).ToList();
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = totalSnapshotNumber,
                EndTimestamp = startTime.AddDays(lastingDays),
                StartTimestamp = startTime,
                Options = { options },
                AcceptedCurrency = EconomicSystemTestConstants.NativeTokenSymbol,
                IsLockToken = isLockToken
            };
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            input.Options.Clear();
            var votingItemId = Hash.FromTwoHashes(Hash.FromMessage(input), Hash.FromMessage(sender));
            return await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = votingItemId
            });
        }
    }
}