using AElf.Kernel.TransactionPool.Application;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class TransactionPoolStatusProfile : Profile
    {
        public TransactionPoolStatusProfile()
        {
            CreateMap<TransactionPoolStatus, GetTransactionPoolStatusOutput>()
                .ForMember(d => d.Queued, opt => opt.MapFrom(s => s.AllTransactionCount))
                .ForMember(d => d.Validated, opt => opt.MapFrom(s => s.ValidatedTransactionCount));

        }
    }
}