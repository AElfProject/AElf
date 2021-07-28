using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.CodeCheck.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.CodeCheck
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;

        public NewIrreversibleBlockFoundEventHandler(ICheckedCodeHashProvider checkedCodeHashProvider)
        {
            _checkedCodeHashProvider = checkedCodeHashProvider;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _checkedCodeHashProvider.RemoveCodeHashAsync(new BlockIndex
            {
                BlockHash = eventData.BlockHash,
                BlockHeight = eventData.BlockHeight
            }, eventData.PreviousIrreversibleBlockHeight);
        }
    }
}