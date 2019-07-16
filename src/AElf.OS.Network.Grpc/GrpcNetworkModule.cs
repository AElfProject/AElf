using System.Collections.Generic;
using System.Net;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkModule : AElfModule
    {
        /// <summary>
        /// Registers the components implemented by the gRPC library.
        /// </summary>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();

            // Internal dependencies
            context.Services.AddTransient<IPeerDialer, PeerDialer>();
            context.Services.AddSingleton<GrpcServerService>();
            context.Services.AddSingleton<AuthInterceptor>();
            context.Services.AddSingleton<RetryInterceptor>();

            // setup the server
            context.Services.AddSingleton<Server>(o =>
            {
                var networkOptions = o.GetService<IOptionsSnapshot<NetworkOptions>>().Value;
                var serverService = o.GetService<GrpcServerService>();
                var authInterceptor = o.GetService<AuthInterceptor>();


                ServerServiceDefinition serviceDefinition = PeerService.BindService(serverService);
                // authentication interceptor
            
                if (authInterceptor != null)
                    serviceDefinition = serviceDefinition.Intercept(authInterceptor);
                
                var server = new Server(new List<ChannelOption>
                {
                    new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                    new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength)
                })
                {
                    Services = {serviceDefinition},
                    Ports =
                    {
                        new ServerPort(IPAddress.Any.ToString(), networkOptions.ListeningPort, ServerCredentials.Insecure)
                    }
                };

                return server;
            });
        }
    }
}