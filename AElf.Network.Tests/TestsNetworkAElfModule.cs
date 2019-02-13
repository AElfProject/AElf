using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Network.Tests
{
    public class TestsNetworkAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IAccountService>(o => Mock.Of<IAccountService>(
                c => c.GetAccountAsync() == Task.FromResult(Address.FromString("AELF_Test")) && c
                         .VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()) ==
                     Task.FromResult(true)));
        }
    }
}