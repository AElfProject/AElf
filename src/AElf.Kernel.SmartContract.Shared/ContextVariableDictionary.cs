using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AElf.Kernel.SmartContract
{
    /// <summary>
    /// Convention: Use ',' as separator.
    /// </summary>
    public class ContextVariableDictionary : ReadOnlyDictionary<string, string>
    {
        public ContextVariableDictionary(IDictionary<string, string> dictionary) : base(dictionary)
        {
        }
    
        public string NativeSymbol => this[nameof(NativeSymbol)];
    
        public const string NativeSymbolName = nameof(NativeSymbol);
    }
}