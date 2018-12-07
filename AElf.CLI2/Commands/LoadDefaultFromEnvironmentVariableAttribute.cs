using System;

namespace AElf.CLI2.Commands
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LoadDefaultFromEnvironmentVariableAttribute : Attribute
    {
        public string VariableName { get; }

        public LoadDefaultFromEnvironmentVariableAttribute(string variableName)
        {
            VariableName = variableName;
        }
    }
}