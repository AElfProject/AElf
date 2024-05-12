using System;

namespace AElf.Kernel;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UmpAttribute : Attribute
{
    public UmpAttribute()
    {
    }
}