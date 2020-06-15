using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;
using Google.Protobuf;

namespace AElf.WebApp.Application.Chain
{
    public class BlockProfile : AutoMapper.Profile
    {
        public const string IncludeTransactions = "IncludeTransactions";
        
        public BlockProfile()
        {
            CreateMap<Block, BlockDto>()
                .ForMember(d => d.BlockHash, opt => opt.MapFrom(s => s.GetHash()))
                .ForMember(destination => destination.BlockSize, opt => opt.MapFrom(source => source.CalculateSize()));

            CreateMap<BlockHeader, BlockHeaderDto>()
                .ForMember(d => d.MerkleTreeRootOfTransactionState,
                    opt => opt.MapFrom(s => s.MerkleTreeRootOfTransactionStatus.ToHex()))
                .ForMember(d => d.Extra, opt => opt.MapFrom(s => s.ExtraData.ToString()))
                .ForMember(d => d.Time, opt => opt.MapFrom(s => s.Time.ToDateTime()))
                .ForMember(d => d.ChainId,
                    opt => opt.MapFrom(s => ChainHelper.ConvertChainIdToBase58(s.ChainId)))
                .ForMember(d => d.Bloom,
                    opt => opt.MapFrom(s =>
                        s.Bloom.Length == 0
                            ? ByteString.CopyFrom(new byte[256]).ToBase64()
                            : s.Bloom.ToBase64()))
                .ForMember(d => d.SignerPubkey,
                    opt => opt.MapFrom(s => s.SignerPubkey.ToByteArray().ToHex(false)));

            CreateMap<BlockBody, BlockBodyDto>()
                .ForMember(d => d.Transactions, opt => opt.MapFrom<BlockBodyResolver>());
        }
    }
    
    public class BlockBodyResolver : IValueResolver<BlockBody, BlockBodyDto, List<string>>
    {
        public List<string> Resolve(BlockBody source, BlockBodyDto destination, List<string> destMember, ResolutionContext context)
        {
            var includeTransactions = (bool) context.Items[BlockProfile.IncludeTransactions];
            return includeTransactions ? source.TransactionIds.Select(t => t.ToHex()).ToList() : null;
        }
    }
}