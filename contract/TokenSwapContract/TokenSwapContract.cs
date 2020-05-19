using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Tokenswap;

namespace TokenSwapContract
{
    public partial class TokenSwapContract : TokenSwapContractContainer.TokenSwapContractBase
    {
        public override Hash CreateSwap(CreateSwapInput input)
        {
            var swapId = HashHelper.ConcatAndCompute(Context.TransactionId, HashHelper.ComputeFrom(input));
            Assert(State.SwapInfo[swapId] == null, "Already added.");
            
            var swapInfo = new SwapInfo
            {
                SwapId = swapId,
                Controller = Context.Sender,
                OriginTokenNumericBigEndian = input.OriginTokenNumericBigEndian,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte
            };
            foreach (var swapTargetToken in input.SwapTargetTokenList)
            {
                AssertSwapTargetToken(swapTargetToken.TargetTokenSymbol);
                var swapPair = new SwapPair
                {
                    SwapId = swapId,
                    OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                    OriginTokenNumericBigEndian = input.OriginTokenNumericBigEndian,
                    TargetTokenSymbol = swapTargetToken.TargetTokenSymbol,
                    SwapRatio = swapTargetToken.SwapRatio,
                    DepositAmount = swapTargetToken.DepositAmount
                };
                AssertValidSwapPair(swapPair);
                var pairId = HashHelper.ConcatAndCompute(swapId, HashHelper.ComputeFrom(swapTargetToken.TargetTokenSymbol));
                swapInfo.SwapTargetTokenMap.Add(swapTargetToken.TargetTokenSymbol, pairId);
                State.SwapPairs[pairId] = swapPair;
                TransferDepositFrom(swapTargetToken.TargetTokenSymbol, swapTargetToken.DepositAmount, Context.Sender);
            }

            
            State.SwapInfo[swapId] = swapInfo;
            
            Context.Fire(new SwapPairAdded
            {
                SwapId = swapId
            });
            return swapId;
        }

        public override Empty CreateSwapRound(CreateSwapRoundInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            Assert(swapInfo.Controller == Context.Sender, "No permission.");
            foreach (var (symbol, pairId) in swapInfo.SwapTargetTokenMap)
            {
                var swapPair = State.SwapPairs[pairId];
                Assert(swapPair.RoundCount == input.RoundId, "Round id not matched.");
                swapPair.RoundCount += 1;
                State.SwapPairs[pairId] = swapPair;
                State.SwapRounds[input.SwapId][symbol][input.RoundId] = new SwapRound
                {
                    SwapId = swapInfo.SwapId,
                    MerkleTreeRoot = input.MerkleTreeRoot,
                    StartTime = Context.CurrentBlockTime
                };
            }
            
            Context.Fire(new SwapRoundUpdated
            {
                MerkleTreeRoot = input.MerkleTreeRoot,
                StartTime = Context.CurrentBlockTime
            });
            return new Empty();
        }

        public override Empty SwapToken(SwapTokenInput input)
        {
            Assert(Context.Sender == input.ReceiverAddress, "Only receiver has permission to swap token.");
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            ValidateSwapTokenInput(input);
            Assert(TryGetOriginTokenAmount(input.OriginAmount, out var amount) && amount > 0,
                "Invalid token swap input.");
            var leafHash = ComputeLeafHash(amount, input.UniqueId, swapInfo, input.ReceiverAddress);
            var computed = input.MerklePath.ComputeRootWithLeafNode(leafHash);
            foreach (var (symbol, pairId) in swapInfo.SwapTargetTokenMap)
            {
                var swapPair = GetTokenSwapPair(pairId);
                Assert(swapPair.RoundCount > input.RoundId, "Round id not matched.");
                var swapRound = State.SwapRounds[input.SwapId][symbol][input.RoundId];
                Assert(computed == swapRound.MerkleTreeRoot, "Failed to swap token.");
                var targetTokenAmount = GetTargetTokenAmount(amount, swapPair.SwapRatio);
                Assert(targetTokenAmount <= swapPair.DepositAmount, "Deposit not enough.");

                // update swap pair and ledger
                swapPair.SwappedAmount = swapPair.SwappedAmount.Add(targetTokenAmount);
                swapPair.SwappedTimes = swapPair.SwappedTimes.Add(1);
                swapRound.SwappedAmount = swapRound.SwappedAmount.Add(targetTokenAmount);
                swapRound.SwappedTimes = swapRound.SwappedTimes.Add(1);
                swapPair.DepositAmount = swapPair.DepositAmount.Sub(targetTokenAmount);
            
                AssertValidSwapPair(swapPair);
                State.SwapPairs[input.SwapId] = swapPair;
                State.Ledger[input.SwapId][input.UniqueId] = targetTokenAmount;

                // transfer
                TransferToken(swapPair.TargetTokenSymbol, targetTokenAmount, input.ReceiverAddress);
                Context.Fire(new TokenSwapEvent
                {
                    Amount = targetTokenAmount,
                    Address = input.ReceiverAddress,
                    Symbol = swapPair.TargetTokenSymbol
                });
            }
            
            return new Empty();
        }

        public override Empty ChangeSwapRatio(ChangeSwapRatioInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            Assert(swapInfo.Controller == Context.Sender, "No permission.");
            Assert(swapInfo.SwapTargetTokenMap.TryGetValue(input.TargetTokenSymbol, out var pairId),
                "Target token not registered.");
            var swapPair = GetTokenSwapPair(pairId);
            swapPair.SwapRatio = input.SwapRatio;
            AssertValidSwapPair(swapPair);
            State.SwapPairs[pairId] = swapPair;
            Context.Fire(new SwapRatioChanged
            {
                SwapId = input.SwapId,
                NewSwapRatio = input.SwapRatio,
                TargetTokenSymbol = input.TargetTokenSymbol
            });
            return new Empty();
        }

        public override SwapInfo GetSwapInfo(Hash input)
        {
            var swapInfo = State.SwapInfo[input];
            return swapInfo;
        }

        public override SwapPair GetSwapPair(GetSwapPairInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            Assert(swapInfo.SwapTargetTokenMap.TryGetValue(input.TargetTokenSymbol, out var pairId),
                "Target token not registered.");
            var swapPair = GetTokenSwapPair(pairId);
            return swapPair;
        }

        public override SwapRound GetSwapRound(GetSwapRoundInput input)
        {
            return State.SwapRounds[input.SwapId][input.TargetTokenSymbol][input.RoundId];
        }

        public override Empty Deposit(DepositInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            Assert(swapInfo.Controller == Context.Sender, "No permission.");
            var swapPairId = swapInfo.SwapTargetTokenMap[input.TargetTokenSymbol];
            var swapPair = GetTokenSwapPair(swapPairId);
            swapPair.DepositAmount = swapPair.DepositAmount.Add(input.Amount);
            AssertValidSwapPair(swapPair);
            State.SwapPairs[swapPairId] = swapPair;
            TransferDepositFrom(swapPair.TargetTokenSymbol, input.Amount, Context.Sender);
            return new Empty();
        }
    }
}