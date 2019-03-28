using System;
using Microsoft.AspNetCore.Http;

namespace AElf.OS.Rpc
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PathAttribute : Attribute
    {
        public PathAttribute(string path)
        {
            Path = new PathString(path);
        }

        public PathString Path { get; }
    }
}