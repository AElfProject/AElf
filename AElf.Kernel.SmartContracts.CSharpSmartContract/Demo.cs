using System;

namespace AElf.Kernel.SmartContracts.CSharpSmartContract
{
    public class Class1
    {
        public string Name { get; }

        public Class1(string name)
        {
            Name = name;
        }

        
        public string SayHello(String yours)
        {
            Console.WriteLine("Hello " + yours + ", I am " + Name);
            return Name;
        }
    }
}