using AElf.Standards.ACS0;
using Google.Protobuf;

namespace AElf.Contracts.Referendum
{
    public static class GenesisContractDtoExtensions
    {
        internal static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            IMessage input)
        {
            systemTransactionMethodCallList.Value.Add(new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
            {
                MethodName = methodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            });
        }
    }
}