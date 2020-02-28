using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Tokenswap;

namespace TokenSwapContract
{
    public partial class TokenSwapContract : TokenSwapContractContainer.TokenSwapContractBase
    {
        public override Hash AddSwapPair(AddSwapPairInput input)
        {
            var tokenInfo = GetTokenInfo(input.TargetTokenSymbol);
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), "Token not found.");

            var pairId = Hash.FromTwoHashes(Context.TransactionId, Hash.FromMessage(input));
            var swapPair = new SwapPair
            {
                PairId = pairId,
                Controller = Context.Sender,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                TargetTokenSymbol = input.TargetTokenSymbol,
                SwapRatio = input.SwapRatio,
                DepositAmount = input.DepositAmount
            };
            AssertValidSwapPair(swapPair);
            State.SwapPairs[pairId] = swapPair;

            TransferDepositFrom(input.TargetTokenSymbol, input.DepositAmount, Context.Sender);
            Context.Fire(new SwapPairAdded
            {
                PairId = pairId
            });
            return pairId;
        }

        public override Empty AddSwapRound(AddSwapRoundInput input)
        {
            var swapPair = GetTokenSwapPair(input.PairId);
            swapPair.CurrentRound = new SwapRound
            {
                PairId = swapPair.PairId,
                MerkleTreeRoot = input.MerkleTreeRoot,
                StartTime = Context.CurrentBlockTime
            };
            State.SwapPairs[input.PairId] = swapPair;
            Context.Fire(new SwapRoundUpdated
            {
                MerkleTreeRoot = input.MerkleTreeRoot,
                StartTime = Context.CurrentBlockTime
            });
            return new Empty();
        }

        public override Empty SwapToken(SwapTokenInput input)
        {
            var swapPair = GetTokenSwapPair(input.PairId);
            ValidateSwapTokenInput(input);

            Assert(TryGetOriginTokenAmount(input.OriginAmount, out var amount) && amount > 0,
                "Invalid token swap input.");
            var leafHash = ComputeLeafHash(amount, input.UniqueId, swapPair, input.ReceiverAddress);
            var computed = input.MerklePath.ComputeRootWithLeafNode(leafHash);
            Assert(computed == swapPair.CurrentRound.MerkleTreeRoot, "Failed to swap token.");
            var targetTokenAmount = GetTargetTokenAmount(amount, swapPair.SwapRatio);
            Assert(targetTokenAmount <= swapPair.DepositAmount, "Deposit not enough.");

            // update swap pair and ledger
            swapPair.SwappedAmount = swapPair.SwappedAmount.Add(targetTokenAmount);
            swapPair.SwappedTimes = swapPair.SwappedTimes.Add(1);
            swapPair.CurrentRound.SwappedAmount = swapPair.CurrentRound.SwappedAmount.Add(targetTokenAmount);
            swapPair.CurrentRound.SwappedTimes = swapPair.CurrentRound.SwappedTimes.Add(1);
            swapPair.DepositAmount = swapPair.DepositAmount.Sub(targetTokenAmount);
            
            AssertValidSwapPair(swapPair);
            State.SwapPairs[input.PairId] = swapPair;
            State.Ledger[input.PairId][input.UniqueId] = targetTokenAmount;

            // transfer
            TransferToken(swapPair.TargetTokenSymbol, targetTokenAmount, input.ReceiverAddress);
            Context.Fire(new TokenSwapEvent
            {
                Amount = targetTokenAmount,
                Address = input.ReceiverAddress,
                Symbol = swapPair.TargetTokenSymbol
            });
            return new Empty();
        }

        public override Empty ChangeSwapRatio(ChainSwapRatioInput input)
        {
            var swapPair = GetTokenSwapPair(input.PairId);
            Assert(swapPair.Controller == Context.Sender, "No permission.");
            swapPair.SwapRatio = input.SwapRatio;
            AssertValidSwapPair(swapPair);
            State.SwapPairs[input.PairId] = swapPair;
            Context.Fire(new SwapRatioChanged
            {
                PairId = input.PairId,
                NewSwapRatio = input.SwapRatio
            });
            return new Empty();
        }

        public override SwapPair GetSwapPair(Hash input)
        {
            var swapPair = State.SwapPairs[input];
            return swapPair;
        }

        public override SwapRound GetCurrentSwapRound(Hash input)
        {
            var swapPair = GetTokenSwapPair(input);
            return swapPair.CurrentRound;
        }

        public override Empty Deposit(DepositInput input)
        {
            var swapPair = GetTokenSwapPair(input.PairId);
            Assert(swapPair.Controller == Context.Sender, "No permission.");
            swapPair.DepositAmount = swapPair.DepositAmount.Add(input.Amount);
            AssertValidSwapPair(swapPair);
            State.SwapPairs[input.PairId] = swapPair;
            TransferDepositFrom(swapPair.TargetTokenSymbol, input.Amount, Context.Sender);
            return new Empty();
        }
    }
}