using System.Collections.Generic;

namespace Solang
{
    public class ArgABI
    {
        public string Label { get; set; }
        public List<ReturnTypeABI> Type { get; set; }
    }
}