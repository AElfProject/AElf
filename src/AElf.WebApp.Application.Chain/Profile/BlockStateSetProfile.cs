using AElf.Kernel;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;

namespace AElf.WebApp.Application.Chain;

public class BlockStateSetProfile : Profile
{
    public BlockStateSetProfile()
    {
        CreateMap<BlockStateSet, BlockStateDto>();
    }
}