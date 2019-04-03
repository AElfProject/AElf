#region Copyright notice and license

// Copyright 2015 gRPC authors. Modified by AElfProject.
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

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AElf.Runtime.CSharp.Tests")]
namespace AElf.Types.CSharp
{
    internal static class ServerCalls
    {
        public static IServerCallHandler UnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler)
            where TRequest : class
            where TResponse : class
        {
            return new UnaryServerCallHandler<TRequest, TResponse>(method, handler);
        }
    }
}
