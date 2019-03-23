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

using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types.CSharp
{
    internal interface IServerCallHandler
    {
        bool IsView();
        byte[] Execute(byte[] input);
        object ReturnBytesToObject(byte[] returnBytes);
        string ReturnBytesToString(byte[] returnBytes);
        object InputBytesToObject(byte[] inputBytes);
        string InputBytesToString(byte[] inputBytes);
    }

    internal class UnaryServerCallHandler<TRequest, TResponse> : IServerCallHandler
        where TRequest : class
        where TResponse : class
    {
        readonly Method<TRequest, TResponse> method;
        readonly UnaryServerMethod<TRequest, TResponse> handler;

        public UnaryServerCallHandler(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler)
        {
            this.method = method;
            this.handler = handler;
        }

        public bool IsView()
        {
            return method.Type == MethodType.View;
        }

        public byte[] Execute(byte[] input)
        {
            var inputObj = method.RequestMarshaller.Deserializer(input);
            var response = handler(inputObj);
            return response != null ? method.ResponseMarshaller.Serializer(response) : null;
        }

        public object ReturnBytesToObject(byte[] returnBytes)
        {
            return method.ResponseMarshaller.Deserializer(returnBytes);
        }

        public string ReturnBytesToString(byte[] returnBytes)
        {
            return method.ResponseMarshaller.Deserializer(returnBytes).ToString();
        }

        public object InputBytesToObject(byte[] inputBytes)
        {
            return method.RequestMarshaller.Deserializer(inputBytes);
        }

        public string InputBytesToString(byte[] inputBytes)
        {
            return method.RequestMarshaller.Deserializer(inputBytes).ToString();
        }
    }
}
