using AElf.Management.Request;

namespace AElf.Management.Models
{
    public class TxPoolSizeResult:JsonRpcResult
    {
        public ulong Result { get; set; }

    }
}