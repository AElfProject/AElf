using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IHandshakeProvider
    {
        Task<Handshake> GetHandshakeAsync();

        Task<bool> ValidateHandshakeAsync(Handshake handshake, string connectionPubkey);
    }
}