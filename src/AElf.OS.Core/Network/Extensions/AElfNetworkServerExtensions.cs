using System.Threading.Tasks;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Extensions
{
    public static class AElfNetworkServerExtensions
    {
        public static async Task<bool> CheckEndpointAvailableAsync(this IAElfNetworkServer networkServer,
            string endpoint)
        {
            if (!AElfPeerEndpointHelper.TryParse(endpoint, out var aelfPeerEndpoint))
            {
                return false;
            }

            return await networkServer.CheckEndpointAvailableAsync(aelfPeerEndpoint);
        }
    }
}