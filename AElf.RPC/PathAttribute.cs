
using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AElf.RPC
{
    [AttributeUsage((AttributeTargets.Class))]
    public class PathAttribute:Attribute
    {
        public PathString Path { get; }
        public PathAttribute(string path)
        {
            Path=new PathString(path);
        }
    }
}