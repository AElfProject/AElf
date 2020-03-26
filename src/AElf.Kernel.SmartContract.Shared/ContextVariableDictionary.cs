using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        
        private readonly Dictionary<string,string[]> _stringArrayDictionary = new Dictionary<string, string[]>();
        
        public IEnumerable<string> GetStringArray(string key)
        {
            if (_stringArrayDictionary.TryGetValue(key, out var stringArray))
                return stringArray;
            if (!ContainsKey(key))
            {
                return new List<string>();
            }
            stringArray = this[key].Split(',').ToArray();
            _stringArrayDictionary[key] = stringArray;
            return stringArray;
        }
    }
}