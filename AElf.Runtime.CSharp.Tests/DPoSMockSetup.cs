using System.IO;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Services;
using Google.Protobuf;
using ServiceStack;
using Path = System.IO.Path;

namespace AElf.Runtime.CSharp.Tests
{
    // ReSharper disable once InconsistentNaming
    public class DPoSMockSetup
    {
        public ISmartContractService SmartContractService;

        // ReSharper disable once InconsistentNaming
        public Hash DPoSContractAddress { get; } = Hash.Generate();

        // ReSharper disable once InconsistentNaming
        public byte[] DPoSContractCode
        {
            get
            {
                byte[] code;
                using (var file = File.OpenRead(
                    Path.GetFullPath("../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/AElf.Contracts.DPoS.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task DeployDPoSContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(DPoSContractCode),
                ContractHash = Hash.Zero
            };

            await SmartContractService.DeployContractAsync(DPoSContractAddress, reg);
        }
    }
}