using System;

namespace AElf.Network.Connection
{
    public interface IMessageWriter : IDisposable
    {
        void Start();
        void EnqueueMessage(Message p, Action<Message> successCallback = null);

        void Close();
    }
}