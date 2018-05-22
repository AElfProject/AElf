using System;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeer
    {
        string IpAddress { get; }
        ushort Port { get; }
    }
}