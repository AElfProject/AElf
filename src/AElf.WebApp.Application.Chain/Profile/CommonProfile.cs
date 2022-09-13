using AElf.Types;
using AutoMapper;
using Google.Protobuf;

namespace AElf.WebApp.Application.Chain;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<Hash, string>()
            .ConvertUsing(s => s == null ? null : s.ToHex());

        CreateMap<Address, string>()
            .ConvertUsing(s => s.ToBase58());

        CreateMap<ByteString, string>()
            .ConvertUsing(s => s.ToBase64());
    }
}