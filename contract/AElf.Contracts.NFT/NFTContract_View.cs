using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        public override NFTProtocolInfo GetNFTProtocolInfo(StringValue input)
        {
            return State.NftProtocolMap[input.Value];
        }

        public override NFTInfo GetNFTInfo(GetNFTInfoInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            return GetNFTInfoByTokenHash(tokenHash);
        }

        public override NFTInfo GetNFTInfoByTokenHash(Hash input)
        {
            var nftInfo = State.NftInfoMap[input];
            var nftProtocolInfo = State.NftProtocolMap[nftInfo.Symbol];
            nftInfo.ProtocolName = nftProtocolInfo.ProtocolName;
            nftInfo.Creator = nftProtocolInfo.Creator;
            nftInfo.BaseUri = nftProtocolInfo.BaseUri;
            nftInfo.NftType = nftProtocolInfo.NftType;
            return nftInfo;
        }

        public override GetBalanceOutput GetBalance(GetBalanceInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            return new GetBalanceOutput
            {
                Owner = input.Owner,
                Balance = State.BalanceMap[tokenHash][input.Owner],
                TokenHash = tokenHash
            };
        }

        public override GetBalanceOutput GetBalanceByTokenHash(GetBalanceByTokenHashInput input)
        {
            return new GetBalanceOutput
            {
                Owner = input.Owner,
                Balance = State.BalanceMap[input.TokenHash][input.Owner],
                TokenHash = input.TokenHash
            };
        }

        public override GetAllowanceOutput GetAllowance(GetAllowanceInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            return new GetAllowanceOutput
            {
                Owner = input.Owner,
                Spender = input.Spender,
                TokenHash = tokenHash,
                Allowance = State.AllowanceMap[tokenHash][input.Owner][input.Spender]
            };
        }

        public override GetAllowanceOutput GetAllowanceByTokenHash(GetAllowanceByTokenHashInput input)
        {
            return new GetAllowanceOutput
            {
                Owner = input.Owner,
                Spender = input.Spender,
                TokenHash = input.TokenHash,
                Allowance = State.AllowanceMap[input.TokenHash][input.Owner][input.Spender]
            };
        }

        public override MinterList GetMinterList(StringValue input)
        {
            return State.MinterListMap[input.Value];
        }

        public override Hash CalculateTokenHash(CalculateTokenHashInput input)
        {
            return CalculateTokenHash(input.Symbol, input.TokenId);
        }

        public override NFTTypes GetNFTTypes(Empty input)
        {
            return State.NFTTypes.Value ?? InitialNFTTypeNameMap();
        }

        private List<string> GetNftMetadataReservedKeys()
        {
            return new List<string>
            {
                NftTypeMetadataKey,
                NftBaseUriMetadataKey,
                AssembledNftsKey,
                AssembledFtsKey,
                NftTokenIdReuseMetadataKey
            };
        }
    }
}