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

        private string GetSymbol(string nftType)
        {
            var randomNumber = GenerateSymbolNumber();
            State.IsCreatedMap[randomNumber] = true;
            var shortName = State.NFTTypeShortNameMap[nftType];
            if (shortName == null)
            {
                InitialNFTTypeNameMap();
                shortName = State.NFTTypeShortNameMap[nftType];
                if (shortName == null)
                {
                    throw new AssertionException($"Short name of NFT Type {nftType} not found.");
                }
            }

            return $"{shortName}{randomNumber}";
        }

        private NFTTypes InitialNFTTypeNameMap()
        {
            if (State.NFTTypes.Value != null)
            {
                return State.NFTTypes.Value;
            }

            var nftTypes = new NFTTypes();
            nftTypes.Value.Add("XX", NFTType.Any.ToString());
            nftTypes.Value.Add("AR", NFTType.Art.ToString());
            nftTypes.Value.Add("MU", NFTType.Music.ToString());
            nftTypes.Value.Add("DN", NFTType.DomainNames.ToString());
            nftTypes.Value.Add("VW", NFTType.VirtualWorlds.ToString());
            nftTypes.Value.Add("TC", NFTType.TradingCards.ToString());
            nftTypes.Value.Add("CO", NFTType.Collectables.ToString());
            nftTypes.Value.Add("SP", NFTType.Sports.ToString());
            nftTypes.Value.Add("UT", NFTType.Utility.ToString());
            nftTypes.Value.Add("BA", NFTType.Badges.ToString());
            State.NFTTypes.Value = nftTypes;

            foreach (var pair in nftTypes.Value)
            {
                State.NFTTypeShortNameMap[pair.Value] = pair.Key;
                State.NFTTypeFullNameMap[pair.Key] = pair.Value;
            }

            return nftTypes;
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