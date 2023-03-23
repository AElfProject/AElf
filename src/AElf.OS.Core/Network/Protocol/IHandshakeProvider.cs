using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.OS.Network.Protocol;

public interface IHandshakeProvider
{
    Task<Handshake> GetHandshakeAsync(int version=KernelConstants.ProtocolVersion);
    Task<HandshakeValidationResult> ValidateHandshakeAsync(Handshake handshake);
}