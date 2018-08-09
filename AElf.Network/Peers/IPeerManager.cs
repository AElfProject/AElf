using System;

namespace AElf.Network.Peers
{
    public interface IPeerManager : IDisposable
    {
        event EventHandler PeerAdded;
        
        void Start();
    }
}