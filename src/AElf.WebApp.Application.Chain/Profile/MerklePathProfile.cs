using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class MerklePathProfile : Profile
    {
        public MerklePathProfile()
        {
            CreateMap<MerklePath, MerklePathDto>();

            CreateMap<MerklePathNode, MerklePathNodeDto>();
        }
    }
}