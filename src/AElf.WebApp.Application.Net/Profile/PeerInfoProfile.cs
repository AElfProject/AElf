using System.Collections.Generic;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Types;
using AElf.WebApp.Application.Net.Dto;
using AutoMapper;

namespace AElf.WebApp.Application.Net
{
    public class PeerInfoProfile : Profile
    {
        public const string WithMetrics = "WithMetrics";
        
        public PeerInfoProfile()
        {
            CreateMap<PeerInfo, PeerDto>()
            .ForMember(d => d.RequestMetrics, opt => opt.MapFrom<PeerInfoResolver>());
        }
    }
    
    public class PeerInfoResolver : IValueResolver<PeerInfo, PeerDto, List<RequestMetric>>
    {
        public List<RequestMetric> Resolve(PeerInfo source, PeerDto destination, List<RequestMetric> destMember, ResolutionContext context)
        {
            var withMetrics = (bool) context.Items[PeerInfoProfile.WithMetrics];
            return withMetrics ? source.RequestMetrics : null;
        }
    }
}