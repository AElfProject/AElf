using System.Linq;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class ChainProfile : AutoMapper.Profile
    {
        public ChainProfile()
        {
            CreateMap<Kernel.Chain, ChainStatusDto>()
                .ForMember(d => d.ChainId, opt => opt.MapFrom(s => ChainHelper.ConvertChainIdToBase58(s.Id)))
                .ForMember(d => d.Branches,
                    opt => opt.MapFrom(s =>
                        s.Branches.ToDictionary(b => Hash.LoadFromBase64(b.Key).ToHex(), b => b.Value)))
                .ForMember(d => d.NotLinkedBlocks,
                    opt => opt.MapFrom(s =>
                        s.NotLinkedBlocks.ToDictionary(b => Hash.LoadFromBase64(b.Key).ToHex(),
                            b => Hash.LoadFromBase64(b.Value).ToHex())))
                .Ignore(d=>d.GenesisContractAddress);
        }
    }
}