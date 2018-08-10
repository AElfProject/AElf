using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    public interface ISmartContract 
    {
    }

    public enum SmartContractType
    {
        BasicContractZero = 0,
        AElfDPoS = 1,
        TokenContract = 2    
    }
}