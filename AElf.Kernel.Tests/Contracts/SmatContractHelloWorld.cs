using System;

namespace AElf.Kernel.Tests.Contracts
{
    public class SmatContractHelloWorld : SmartContract
    {
        public void HelloWorld(string name)
        {
            Console.WriteLine("Hello World " + name);
        }
    }
}