using System.Linq;
using AElf.Types;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.MessageQueue
{
    public class WebAppMsmqAutoMapperProfile : Profile
    {
        public WebAppMsmqAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
            * Alternatively, you can split your mapping configurations
            * into multiple profile classes for a better organization. */
            CreateMap<LogEvent, LogEventEto>()
                .ForMember(destination => destination.Address,
                    opt => opt.MapFrom(source => source.Address.ToBase58()))
                .ForMember(destination => destination.Indexed,
                    opt => opt.MapFrom(source => source.Indexed.Select(i => i.ToBase64()).ToArray()))
                .ForMember(destination => destination.NonIndexed,
                    opt => opt.MapFrom(source => source.NonIndexed.ToBase64()));

            CreateMap<TransactionResult, TransactionResultEto>()
                .ForMember(destination => destination.TransactionId,
                    opt => opt.MapFrom(source => source.TransactionId.ToHex()))
                .ForMember(destination => destination.Status,
                    opt => opt.MapFrom(source => source.Status.ToString().ToUpper()))
                .ForMember(destination => destination.ReturnValue,
                    opt => opt.MapFrom(source => source.ReturnValue.ToHex(false)))
                .ForMember(destination => destination.BlockHash,
                    opt => opt.MapFrom(source => source.BlockHash.ToHex()))
                .Ignore(destination => destination.BlockTime)
                .Ignore(destination => destination.FromAddress)
                .Ignore(destination => destination.ToAddress);
        }
    }
}