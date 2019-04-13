#region Copyright notice and license

// Copyright 2019 The gRPC Authors. Modified by AElfProject.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Google.Protobuf.Reflection;

namespace AElf.CSharp.Core
{
    internal static class ServerServiceDefinitionExtensions
    {
        /// <summary>
        /// Maps methods from <c>ServerServiceDefinition</c> to server call handlers.
        /// </summary>
        internal static ReadOnlyDictionary<string, IServerCallHandler> GetCallHandlers(this ServerServiceDefinition serviceDefinition)
        {
            var binder = new DefaultServiceBinder();
            serviceDefinition.BindService(binder);
            return binder.GetCallHandlers();
        }

        internal static IReadOnlyList<ServiceDescriptor> GetDescriptors(this ServerServiceDefinition serviceDefinition)
        {
            var binder = new DefaultServiceBinder();
            serviceDefinition.BindService(binder);
            return binder.GetDescriptors();
        }

        /// <summary>
        /// Helper for converting <c>ServerServiceDefinition</c> to server call handlers.
        /// </summary>
        private class DefaultServiceBinder : ServiceBinderBase
        {
            readonly List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();
            readonly Dictionary<string, IServerCallHandler> callHandlers = new Dictionary<string, IServerCallHandler>();

            internal ReadOnlyDictionary<string, IServerCallHandler> GetCallHandlers()
            {
                return new ReadOnlyDictionary<string, IServerCallHandler>(this.callHandlers);
            }

            internal IReadOnlyList<ServiceDescriptor> GetDescriptors()
            {
                return descriptors.AsReadOnly();
            }

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method,
                UnaryServerMethod<TRequest, TResponse> handler)
            {
                callHandlers.Add(method.Name, ServerCalls.UnaryCall(method, handler));
            }

            public override void AddDescriptor(ServiceDescriptor descriptor)
            {
                descriptors.Add(descriptor);
            }
        }
    }
}
