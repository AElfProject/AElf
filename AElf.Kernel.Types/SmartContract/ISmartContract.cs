using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    public interface ISmartContract 
    {
    }

    public enum SmartContractType
    {
        // type name should be contained in the contract name  
        BasicContractZero = 0,
        AElfDPoS = 1,
        TokenContract = 2,
        SideChainContract = 3
    }
}