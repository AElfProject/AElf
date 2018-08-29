using System;

namespace AElf.Network.Sim
{
    public interface INetNode
    {
        event EventHandler EventReceived;

        void Stop();
    }
}