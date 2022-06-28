using System.Linq;
using AElf.Kernel;
using AElf.Types;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Provider;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.MessageQueue;

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
                opt => opt.MapFrom(source => source.NonIndexed.ToBase64()))
            .ForMember(destination => destination.Bloom,
                opt => opt.MapFrom(source => source.GetBloom().Data.ToHex(false)));

        CreateMap<TransactionResult, TransactionMessageEto>()
            .ForMember(destination => destination.TransactionId,
                opt => opt.MapFrom(source => source.TransactionId.ToHex()))
            .ForMember(destination => destination.Status,
                opt => opt.MapFrom(source => source.Status.ToString().ToUpper()))
            .ForMember(destination => destination.ReturnValue,
                opt => opt.MapFrom(source => source.ReturnValue.ToHex(false)))
            .ForMember(destination => destination.Bloom,
                opt => opt.MapFrom(source => source.Bloom.ToHex(false)));

        CreateMap<Block, BlockMessageEto>()
            .Ignore(x => x.TransactionMessageList)
            .ForMember(destination => destination.ChainId,
                opt => opt.MapFrom(source => source.Header.ChainId))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Header.GetHash().ToHex()))
            .ForMember(destination => destination.BlockTime,
                opt => opt.MapFrom(source => source.Header.Time.ToDateTime()))
            .ForMember(destination => destination.Bloom,
                opt => opt.MapFrom(source => source.Header.Bloom.ToHex(false)));

        CreateMap<SyncInformation, SyncInformationDto>()
            .ForMember(destination => destination.State,
                opt => opt.MapFrom(source => source.State.ToString()));
    }
}