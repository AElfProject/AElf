using System;

namespace AElf.Network.Connection
{
    public interface IMessageWriter : IDisposable
    {
        void Start();
        void EnqueueMessage(Message p);

        void Close();
    }
}