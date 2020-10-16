using System.IO;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class TransactionProfile : Profile
    {
        public const string ErrorTrace = "WithMetrics";

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
                .ForMember(d => d.Error, opt => opt.MapFrom<TransactionErrorResolver>())
                .Ignore(d => d.Transaction)
                .Ignore(d => d.TransactionSize);

            CreateMap<LogEvent, LogEventDto>();
        }
    }

    public class TransactionErrorResolver : IValueResolver<TransactionResult, TransactionResultDto, string>
    {
        public string Resolve(TransactionResult source, TransactionResultDto destination, string destMember,
            ResolutionContext context)
        {
            var errorTraceNeeded = (bool) context.Items[TransactionProfile.ErrorTrace];
            return TakeErrorMessage(source.Error, errorTraceNeeded);
        }

        public static string TakeErrorMessage(string transactionResultError, bool errorTraceNeeded)
        {
            if (string.IsNullOrWhiteSpace(transactionResultError))
                return null;
        
            if (errorTraceNeeded)
                return transactionResultError;

            using var stringReader = new StringReader(transactionResultError);
            return stringReader.ReadLine();
        }
    }
}