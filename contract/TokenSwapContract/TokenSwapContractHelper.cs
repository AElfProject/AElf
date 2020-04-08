using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Tokenswap;

namespace TokenSwapContract
{
    public partial class TokenSwapContract
    {
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

        private SwapPair GetTokenSwapPair(Hash PairId)
        {
            var swapPair = State.SwapPairs[PairId];
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
            Assert(State.Ledger[swapTokenInput.PairId][swapTokenInput.UniqueId] == 0, "Already claimed.");
        }

        private Hash GetHashTokenAmountData(decimal amount, int originTokenSizeInByte, bool isBigEndian)
        {
            var preHolderSize = originTokenSizeInByte - 16;
            int[] amountInIntegers;
            if (isBigEndian)
            {
                amountInIntegers = decimal.GetBits(amount).Reverse().ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte / 4).ToArray();
            }
            else
            {
                amountInIntegers = decimal.GetBits(amount).ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.Take(originTokenSizeInByte / 4).ToArray();
            }

            var amountBytes = new List<byte>();

            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                cur.AddRange(i.ToBytes(isBigEndian));
                return cur;
            });

            if (preHolderSize > 0)
            {
                var placeHolder = Enumerable.Repeat(new byte(), preHolderSize).ToArray();
                amountBytes = isBigEndian
                    ? placeHolder.Concat(amountBytes).ToList()
                    : amountBytes.Concat(placeHolder).ToList();
            }

            return Hash.FromRawBytes(amountBytes.ToArray());
        }

        private Hash GetHashFromAddressData(Address receiverAddress)
        {
            return Hash.FromString(receiverAddress.GetFormatted());
        }

        private Hash ComputeLeafHash(decimal amount, Hash uniqueId, SwapPair swapPair, Address receiverAddress)
        {
            var hashFromAmount = GetHashTokenAmountData(amount, swapPair.OriginTokenSizeInByte,
                swapPair.OriginTokenNumericBigEndian);
            var hashFromAddress = GetHashFromAddressData(receiverAddress);
            return HashHelper.ConcatAndCompute(hashFromAmount, hashFromAddress, uniqueId);
        }

        private long GetTargetTokenAmount(decimal amount, SwapRatio swapRatio)
        {
            var expected = amount * swapRatio.TargetShare / swapRatio.OriginShare;
            var targetTokenAmount = decimal.ToInt64(expected);
            return targetTokenAmount;
        }

        private void TransferDepositFrom(string symbol, long amount, Address address)
        {
            RequireTokenContractStateSet();
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Amount = amount,
                From = address,
                To = Context.Self,
                Symbol = symbol,
                Memo = "Token swap contract deposit."
            });
        }

        private void AssertSwapTargetToken(string symbol)
        {
            var tokenInfo = GetTokenInfo(symbol);
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), "Token not found.");
        }
    }
}