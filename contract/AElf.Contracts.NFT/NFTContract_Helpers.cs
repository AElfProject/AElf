using System.Collections.Generic;
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
            var randomNumber = GenerateSymbolNumber();
            State.IsCreatedMap[randomNumber] = true;
            var shortName = State.NFTTypeShortNameMap[nftType.ToString()];
            if (shortName == null)
            {
                InitialNFTTypeNameMap();
                shortName = State.NFTTypeShortNameMap[nftType.ToString()];
                if (shortName == null)
                {
                    throw new AssertionException($"Short name of NFT Type {nftType.ToString()} not found.");
                }
            }

            return $"{shortName}{randomNumber}";
        }

        private void InitialNFTTypeNameMap()
        {
            State.NFTTypeShortNameMap[NFTType.Any.ToString()] = "XX";
            State.NFTTypeShortNameMap[NFTType.Art.ToString()] = "AR";
            State.NFTTypeShortNameMap[NFTType.Music.ToString()] = "MU";
            State.NFTTypeShortNameMap[NFTType.DomainNames.ToString()] = "DN";
            State.NFTTypeShortNameMap[NFTType.VirtualWorlds.ToString()] = "VW";
            State.NFTTypeShortNameMap[NFTType.TradingCards.ToString()] = "TC";
            State.NFTTypeShortNameMap[NFTType.Collectables.ToString()] = "CO";
            State.NFTTypeShortNameMap[NFTType.Sports.ToString()] = "SP";
            State.NFTTypeShortNameMap[NFTType.Utility.ToString()] = "UT";
            State.NFTTypeShortNameMap[NFTType.Badges.ToString()] = "BA";

            State.NFTTypeFullNameMap["XX"] = NFTType.Any.ToString();
            State.NFTTypeFullNameMap["AR"] = NFTType.Art.ToString();
            State.NFTTypeFullNameMap["MU"] = NFTType.Music.ToString();
            State.NFTTypeFullNameMap["DN"] = NFTType.DomainNames.ToString();
            State.NFTTypeFullNameMap["VW"] = NFTType.VirtualWorlds.ToString();
            State.NFTTypeFullNameMap["TC"] = NFTType.TradingCards.ToString();
            State.NFTTypeFullNameMap["CO"] = NFTType.Collectables.ToString();
            State.NFTTypeFullNameMap["SP"] = NFTType.Sports.ToString();
            State.NFTTypeFullNameMap["UT"] = NFTType.Utility.ToString();
            State.NFTTypeFullNameMap["BA"] = NFTType.Badges.ToString();
        }

        private long GenerateSymbolNumber()
        {
            var length = GetCurrentNumberLength();
            var from = 1L;
            for (var i = 1; i < length; i++)
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

            var flag = State.NftProtocolNumberFlag.Value;

            if (flag == 0)
            {
                // Initial protocol number flag.
                var protocolNumber = 1;
                for (var i = 1; i < State.CurrentSymbolNumberLength.Value; i++)
                {
                    protocolNumber = protocolNumber.Mul(10);
                }

                State.NftProtocolNumberFlag.Value = protocolNumber;
                flag = protocolNumber;
            }

            var upperNumberFlag = flag.Mul(2);
            if (upperNumberFlag.ToString().Length > State.CurrentSymbolNumberLength.Value)
            {
                var newSymbolNumberLength = State.CurrentSymbolNumberLength.Value.Add(1);
                State.CurrentSymbolNumberLength.Value = newSymbolNumberLength;
                var protocolNumber = 1;
                for (var i = 1; i < newSymbolNumberLength; i++)
                {
                    protocolNumber = protocolNumber.Mul(10);
                }

                State.NftProtocolNumberFlag.Value = protocolNumber;
                return newSymbolNumberLength;
            }

            return State.CurrentSymbolNumberLength.Value;
        }

        private void AssertMetadataKeysAreCorrect(IEnumerable<string> metadataKeys)
        {
            var reservedMetadataKey = GetNftMetadataReservedKeys();
            foreach (var metadataKey in metadataKeys)
            {
                Assert(!reservedMetadataKey.Contains(metadataKey), $"Metadata key {metadataKey} is reserved.");
            }
        }
    }
}