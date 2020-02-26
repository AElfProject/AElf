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
            AssertValidOriginToken(input);
            State.SwapPairs[pairId] = new SwapPair
            {
                SwapPairId = pairId,
                Controller = Context.Sender,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                TargetTokenSymbol = input.TargetTokenSymbol,
                SwapRatio = input.SwapRatio
            };
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
            return new Empty();
        }

        public override Empty SwapToken(SwapTokenInput input)
        {
            var swapPair = GetTokenSwapPair(input.SwapPairId);
            ValidateSwapTokenInput(input);
            Assert(TryGetOriginTokenAmount(input.OriginAmount, out var amount), "Invalid token swap input.");

            var leafHash = ComputeLeafHash(amount, input.UniqueId, swapPair);
            var computed = input.MerklePath.ComputeRootWithLeafNode(leafHash);
            Assert(computed == swapPair.CurrentRound.MerkleTreeRoot);
            return new Empty();
        }

        public override Empty ChangeSwapRatio(SwapRatio input)
        {
            return base.ChangeSwapRatio(input);
        }

        public override SwapPair GetSwapPair(Hash input)
        {
            return base.GetSwapPair(input);
        }

        public override SwapRound GetCurrentSwapRound(Empty input)
        {
            return base.GetCurrentSwapRound(input);
        }

        private TokenInfo GetTokenInfo(string symbol)
        {
            if (State.TokenContract.Value == null)
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput {Symbol = symbol});
        }

        private SwapPair GetTokenSwapPair(Hash swapPairId)
        {
            var swapPair = State.SwapPairs[swapPairId];
            Assert(swapPair != null, "Token swap pair not found.");
            return swapPair;
        }


        private void AssertValidOriginToken(AddSwapPairInput input)
        {
            Assert(
                input.OriginTokenSizeInByte <= MaximalOriginTokenRangeSizeInByte &&
                input.OriginTokenSizeInByte >= MinimalOriginTokenRangeSizeInByte, "Invalid origin token.");
            var expectedSize = MaximalOriginTokenRangeSizeInByte;
            while (expectedSize >= MinimalOriginTokenRangeSizeInByte)
            {
                if (input.OriginTokenSizeInByte == expectedSize)
                    return;
                expectedSize >>= 1;
            }

            Assert(false, "Invalid origin token.");
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
                amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte).ToArray();

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

        private Hash GetHashFromAddressData()
        {
            return Hash.FromString(Context.Sender.GetFormatted());
        }

        private Hash ComputeLeafHash(decimal amount, Hash uniqueId, SwapPair swapPair)
        {
            var hashFromAmount = GetHashTokenAmountData(amount, swapPair.OriginTokenSizeInByte);
            var hashFromAddress = GetHashFromAddressData();
            return HashHelper.ConcatAndCompute(hashFromAmount, hashFromAddress, uniqueId);
        }
    }
}