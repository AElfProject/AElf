using AElf.WebApp.Application.Chain;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using FileDescriptorSet = AElf.Runtime.CSharp.FileDescriptorSet;

namespace AElf.WebApp.Application.FullNode.Services;

public interface IContractMethodAppService
{
    Task<string[]> GetContractViewMethodListAsync(string address);
}

public class ContractMethodAppService : ApplicationService, IContractMethodAppService
{
    private static IContractFileDescriptorSetAppService _contractFileDescriptorSetAppService;

    public ILogger<ContractMethodAppService> Logger { get; set; }

    public ContractMethodAppService(IContractFileDescriptorSetAppService contractFileDescriptorSetAppService)
    {
        _contractFileDescriptorSetAppService = contractFileDescriptorSetAppService;
    }

    /// <summary>
    ///     Get the view method list of a contract
    /// </summary>
    /// <param name="address">contract address</param>
    /// <returns></returns>
    public async Task<string[]> GetContractViewMethodListAsync(string address)
    {
        try
        {
            var set = new FileDescriptorSet();
            var fds = await _contractFileDescriptorSetAppService.GetContractFileDescriptorSetAsync(address);
            set.MergeFrom(ByteString.CopyFrom(fds));
            var fdList = FileDescriptor.BuildFromByteStrings(set.File, new ExtensionRegistry
            {
                OptionsExtensions.IsView,
            });
            var viewMethodList =
                (from fd in fdList
                    from service in fd.Services
                    from method in service.Methods
                    where method.GetOptions().GetExtension(OptionsExtensions.IsView)
                    select method.Name).ToList();
            return viewMethodList.ToArray();
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error during GetContractViewMethodListAsync");
            throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
        }
    }
}