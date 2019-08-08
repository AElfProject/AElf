using System.Threading.Tasks;

namespace AElf.OS.Network.Protocol
{
    public interface IHandshakeProvider
    {
        Task<Handshake> GetHandshakeAsync();

        Task<bool> ValidateHandshakeAsync(Handshake handshake);
    }
}