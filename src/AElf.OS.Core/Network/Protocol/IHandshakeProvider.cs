using System.Threading.Tasks;

namespace AElf.OS.Network.Protocol
{
    public interface IHandshakeProvider
    {
        Task<Handshake> GetHandshakeAsync(bool withSecureEndpoint);
        Task<HandshakeValidationResult> ValidateHandshakeAsync(Handshake handshake);
    }
}