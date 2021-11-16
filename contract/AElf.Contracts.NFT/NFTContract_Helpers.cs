using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        private void MakeSureTokenContractAddressSet()
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }
        }

        private void MakeSureRandomNumberProviderContractAddressSet()
        {
            if (State.RandomNumberProviderContract.Value == null)
            {
                State.RandomNumberProviderContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }
        }

        private string GetSymbol(NFTType nftType)
        {
            if (State.NftProtocolNumber.Value == 0)
            {
                State.NftProtocolNumber.Value = 10000000;
            }

            var number = GenerateSymbolNumber();
            State.IsCreatedMap[number] = true;
            return $"{GetPrefix(nftType)}{number}";
        }

        private string GetPrefix(NFTType nftType)
        {
            switch (nftType)
            {
                case NFTType.Any:
                    return "XX";
                case NFTType.Art:
                    return "AR";
                case NFTType.Music:
                    return "MU";
                case NFTType.DomainNames:
                    return "DN";
                case NFTType.VirtualWorlds:
                    return "VW";
                case NFTType.TradingCards:
                    return "TC";
                case NFTType.Collectables:
                    return "CO";
                case NFTType.Sports:
                    return "SP";
                case NFTType.Utility:
                    return "UT";
                case NFTType.Certificate:
                    return "CE";
            }

            return "XX";
        }

        private long GenerateSymbolNumber()
        {
            var length = GetCurrentNumberLength();
            var from = 1L;
            for (var i = 0; i < length; i++)
            {
                from = from.Mul(10);
            }

            var randomBytes = State.RandomNumberProviderContract.GetRandomBytes.Call(new Int64Value
            {
                Value = Context.CurrentHeight.Sub(1)
            }.ToBytesValue());
            var randomHash =
                HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Sender),
                    HashHelper.ComputeFrom(randomBytes));
            long randomNumber;
            do
            {
                randomNumber = Context.ConvertHashToInt64(randomHash, from, from.Mul(10));
            } while (State.IsCreatedMap[randomNumber]);

            return randomNumber;
        }

        private int GetCurrentNumberLength()
        {
            if (State.CurrentSymbolNumberLength.Value == 0)
            {
                State.CurrentSymbolNumberLength.Value = NumberMinLength;
            }

            var currentCount = State.NftProtocolNumber.Value;
            var upper = currentCount.Mul(3).Div(2);
            if (upper.ToString().Length > State.CurrentSymbolNumberLength.Value)
            {
                State.CurrentSymbolNumberLength.Value = upper.ToString().Length;
                return upper.ToString().Length;
            }

            return State.CurrentSymbolNumberLength.Value;
        }
    }
}