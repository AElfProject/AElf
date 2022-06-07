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

using AElf.CSharp.Core.Utils;

namespace AElf.CSharp.Core;

public enum MethodType
{
    /// <summary>The method modifies the contrac state.</summary>
    Action,

    /// <summary>The method doesn't modify the contract state.</summary>
    View
}

/// <summary>
///     A non-generic representation of a remote method.
/// </summary>
public interface IMethod
{
    /// <summary>
    ///     Gets the type of the method.
    /// </summary>
    MethodType Type { get; }

    /// <summary>
    ///     Gets the name of the service to which this method belongs.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    ///     Gets the unqualified name of the method.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the fully qualified name of the method. On the server side, methods are dispatched
    ///     based on this name.
    /// </summary>
    string FullName { get; }
}

/// <summary>
///     A description of a remote method.
/// </summary>
/// <typeparam name="TRequest">Request message type for this method.</typeparam>
/// <typeparam name="TResponse">Response message type for this method.</typeparam>
public class Method<TRequest, TResponse> : IMethod
{
    /// <summary>
    ///     Initializes a new instance of the <c>Method</c> class.
    /// </summary>
    /// <param name="type">Type of method.</param>
    /// <param name="serviceName">Name of service this method belongs to.</param>
    /// <param name="name">Unqualified name of the method.</param>
    /// <param name="requestMarshaller">Marshaller used for request messages.</param>
    /// <param name="responseMarshaller">Marshaller used for response messages.</param>
    public Method(MethodType type, string serviceName, string name, Marshaller<TRequest> requestMarshaller,
        Marshaller<TResponse> responseMarshaller)
    {
        this.Type = type;
        this.ServiceName = Preconditions.CheckNotNull(serviceName, "serviceName");
        this.Name = Preconditions.CheckNotNull(name, "name");
        this.RequestMarshaller = Preconditions.CheckNotNull(requestMarshaller, "requestMarshaller");
        this.ResponseMarshaller = Preconditions.CheckNotNull(responseMarshaller, "responseMarshaller");
        FullName = GetFullName(serviceName, name);
    }

    /// <summary>
    ///     Gets the marshaller used for request messages.
    /// </summary>
    public Marshaller<TRequest> RequestMarshaller { get; }

    /// <summary>
    ///     Gets the marshaller used for response messages.
    /// </summary>
    public Marshaller<TResponse> ResponseMarshaller { get; }

    /// <summary>
    ///     Gets the type of the method.
    /// </summary>
    public MethodType Type { get; }

    /// <summary>
    ///     Gets the name of the service to which this method belongs.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    ///     Gets the unqualified name of the method.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the fully qualified name of the method. On the server side, methods are dispatched
    ///     based on this name.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    ///     Gets full name of the method including the service name.
    /// </summary>
    internal static string GetFullName(string serviceName, string methodName)
    {
        return "/" + serviceName + "/" + methodName;
    }
}