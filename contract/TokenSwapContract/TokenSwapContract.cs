using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
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
                SwapPairId = pairId,
                Controller = Context.Sender,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                TargetTokenSymbol = input.TargetTokenSymbol,
                SwapRatio = input.SwapRatio
            };
            AssertValidSwapPair(swapPair);
            State.SwapPairs[pairId] = swapPair;
            Context.Fire(new SwapPairAdded
            {
                PairId = pairId
            });
            return pairId;
        }

        public override Empty AddSwapRound(AddSwapRoundInput input)
        {
            var swapPair = GetTokenSwapPair(input.SwapPairId);
            swapPair.CurrentRound = new SwapRound
            {
                SwapPairId = swapPair.SwapPairId,
                MerkleTreeRoot = input.MerkleTreeRoot,
                StartTime = Context.CurrentBlockTime
            };
            State.SwapPairs[input.SwapPairId] = swapPair;
            Context.Fire(new SwapRoundUpdated
            {
                MerkleTreeRoot = input.MerkleTreeRoot,
                StartTime = Context.CurrentBlockTime
            });
            return new Empty();
        }

        public override Empty SwapToken(SwapTokenInput input)
        {
            var swapPair = GetTokenSwapPair(input.SwapPairId);
            ValidateSwapTokenInput(input);
            Assert(TryGetOriginTokenAmount(input.OriginAmount, out var amount) && amount > 0,
                "Invalid token swap input.");
            var leafHash = ComputeLeafHash(amount, input.UniqueId, swapPair, input.ReceiverAddress);
            var computed = input.MerklePath.ComputeRootWithLeafNode(leafHash);
            Assert(computed == swapPair.CurrentRound.MerkleTreeRoot, "Failed to swap token.");
            var targetTokenAmount = GetTargetTokenAmount(amount, swapPair.SwapRatio);

            // update swap pair
            swapPair.SwappedAmount = swapPair.SwappedAmount.Add(targetTokenAmount);
            swapPair.SwappedTimes = swapPair.SwappedTimes.Add(1);
            swapPair.CurrentRound.SwappedAmount = swapPair.CurrentRound.SwappedAmount.Add(targetTokenAmount);
            swapPair.CurrentRound.SwappedTimes = swapPair.CurrentRound.SwappedTimes.Add(1);
            State.SwapPairs[input.SwapPairId] = swapPair;
            
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

        private TokenInfo GetTokenInfo(string symbol)
        {
            RequireTokenContractStateSet();
            return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput {Symbol = symbol});
        }

        private void TransferToken(string symbol, long amount, Address to)
        {
            RequireTokenContractStateSet();
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = amount,
                Symbol = symbol,
                To = to,
                Memo = "Token swap."
            });
        }

        private void RequireTokenContractStateSet()
        {
            if (State.TokenContract.Value != null)
                return;

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        }

        private SwapPair GetTokenSwapPair(Hash swapPairId)
        {
            var swapPair = State.SwapPairs[swapPairId];
            Assert(swapPair != null, "Token swap pair not found.");
            return swapPair;
        }

        private void AssertValidSwapPair(SwapPair swapPair)
        {
            Assert(ValidOriginToken(swapPair) && ValidateSwapRatio(swapPair.SwapRatio), "Invalid swap pair.");
        }

        private bool ValidateSwapRatio(SwapRatio swapRatio)
        {
            return swapRatio.OriginShare > 0 && swapRatio.TargetShare > 0;
        }

        private bool ValidOriginToken(SwapPair swapPair)
        {
            Assert(
                swapPair.OriginTokenSizeInByte <= MaximalOriginTokenRangeSizeInByte &&
                swapPair.OriginTokenSizeInByte >= MinimalOriginTokenRangeSizeInByte, "Invalid origin token.");
            var expectedSize = MaximalOriginTokenRangeSizeInByte;
            while (expectedSize >= MinimalOriginTokenRangeSizeInByte)
            {
                if (swapPair.OriginTokenSizeInByte == expectedSize)
                    return true;
                expectedSize >>= 1;
            }

            return false;
        }

        private bool TryGetOriginTokenAmount(string amountInString, out decimal amount)
        {
            return decimal.TryParse(amountInString, out amount);
        }

        private bool IsValidAmount(string amountInString)
        {
            return !string.IsNullOrEmpty(amountInString) && amountInString.First() != '0' &&
                   amountInString.All(character => character >= '0' && character <= '9');
        }

        private void ValidateSwapTokenInput(SwapTokenInput swapTokenInput)
        {
            var amountInString = swapTokenInput.OriginAmount;
            var validationResult = amountInString.Length > 0 && IsValidAmount(swapTokenInput.OriginAmount);
            Assert(validationResult, "Invalid token swap input.");
        }

        private Hash GetHashTokenAmountData(decimal amount, int originTokenSizeInByte)
        {
            var preHolderSize = originTokenSizeInByte - 16;
            var amountInIntegers = decimal.GetBits(amount).Reverse().ToArray();

            if (preHolderSize < 0)
                amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte / 4).ToArray();

            var amountBytes = new List<byte>();
            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                while (cur.Count < preHolderSize)
                {
                    cur.Add(new byte());
                }

                cur.AddRange(i.ToBytes());
                return cur;
            });
            return Hash.FromRawBytes(amountBytes.ToArray());
        }

        private Hash GetHashFromAddressData(Address receiverAddress)
        {
            return Hash.FromString(receiverAddress.GetFormatted());
        }

        private Hash ComputeLeafHash(decimal amount, Hash uniqueId, SwapPair swapPair, Address receiverAddress)
        {
            var hashFromAmount = GetHashTokenAmountData(amount, swapPair.OriginTokenSizeInByte);
            var hashFromAddress = GetHashFromAddressData(receiverAddress);
            return HashHelper.ConcatAndCompute(hashFromAmount, hashFromAddress, uniqueId);
        }

        private long GetTargetTokenAmount(decimal amount, SwapRatio swapRatio)
        {
            var expected = amount * swapRatio.TargetShare / swapRatio.OriginShare;
            var targetTokenAmount = decimal.ToInt64(expected);
            return targetTokenAmount;
        }
    }
}