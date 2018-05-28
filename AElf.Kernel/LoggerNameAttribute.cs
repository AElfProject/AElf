using System;

namespace AElf.Kernel
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class LoggerNameAttribute : Attribute
    {
        public string Name { get; set; }   
        
        public LoggerNameAttribute(string name)
        {
            Name = name;
        }
    }
}