using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IHandshakeProvider
    {
        Task<Handshake> GetHandshakeAsync();
    }
}