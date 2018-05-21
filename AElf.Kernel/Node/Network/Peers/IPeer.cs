using System;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeer
    {
        string IpAddress { get; set; }
        UInt16 Port { get; set; }
    }
}