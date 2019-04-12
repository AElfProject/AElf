using System;
using Microsoft.AspNetCore.Http;

namespace AElf.OS.Rpc
{
    [AttributeUsage((AttributeTargets.Class))]
    public class PathAttribute : Attribute
    {
        public PathString Path { get; }

        public PathAttribute(string path)
        {
            Path = new PathString(path);
        }
    }
}