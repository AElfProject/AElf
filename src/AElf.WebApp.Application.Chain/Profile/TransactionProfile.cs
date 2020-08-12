using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;
using Google.Protobuf;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, TransactionDto>();

            CreateMap<TransactionResult, TransactionResultDto>()
                .ForMember(d => d.ReturnValue, opt => opt.MapFrom(s => s.ReturnValue.ToHex(false)))
                .ForMember(d => d.Bloom,
                    opt => opt.MapFrom(s =>
                        s.Status == TransactionResultStatus.NotExisted
                            ? null
                            : s.Bloom.Length == 0
                                ? ByteString.CopyFrom(new byte[256]).ToBase64()
                                : s.Bloom.ToBase64()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString().ToUpper()))
                .Ignore(d => d.Transaction)
                .Ignore(d => d.TransactionSize);

            CreateMap<LogEvent, LogEventDto>();
        }
    }
}