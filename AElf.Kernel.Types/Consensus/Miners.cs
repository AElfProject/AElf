using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Miners
    {
        public bool IsEmpty()
        {
            return !Nodes.Any();
        }
    }
}