using System.Collections.Generic;
using AElf.CSharp.Core;
using Google.Protobuf.Reflection;

namespace AElf.CSharp.CodeOps
{
    public class DescriptorOnlyServiceBinder : ServiceBinderBase
    {
        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();

        internal IReadOnlyList<ServiceDescriptor> GetDescriptors()
        {
            return _descriptors.AsReadOnly();
        }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse> handler)
        {
            // Do nothing, just an empty implementation to prevent exception, we don't need methods
        }

        public override void AddDescriptor(ServiceDescriptor descriptor)
        {
            _descriptors.Add(descriptor);
        }
    }
}
