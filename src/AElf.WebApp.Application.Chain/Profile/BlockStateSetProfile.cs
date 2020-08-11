using AElf.Kernel;
using AElf.WebApp.Application.Chain.Dto;

namespace AElf.WebApp.Application.Chain
{
    public class BlockStateSetProfile: AutoMapper.Profile
    {
        public BlockStateSetProfile()
        {
            CreateMap<BlockStateSet, BlockStateDto>();
        }
    }
}