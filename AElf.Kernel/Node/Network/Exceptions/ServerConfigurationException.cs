using System;

namespace AElf.Kernel.Node.Network.Exceptions
{
    public class ServerConfigurationException : Exception
    {
        public ServerConfigurationException(string msg, Exception inner) : base(msg, inner) { }
        public ServerConfigurationException(string msg) : base(msg) { }
    }
}