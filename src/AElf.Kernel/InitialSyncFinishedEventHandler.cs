using AElf.Kernel.CodeCheck;
using AElf.Kernel.Node.Events;
using Microsoft.Extensions.Options;

namespace AElf.Kernel;

public class InitialSyncFinishedEventHandler : ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
{
    private readonly CodeCheckOptions _codeCheckOptions;

    public InitialSyncFinishedEventHandler(IOptionsMonitor<CodeCheckOptions> codeCheckOptionsMonitor)
    {
        _codeCheckOptions = codeCheckOptionsMonitor.CurrentValue;
    }

    public Task HandleEventAsync(InitialSyncFinishedEvent eventData)
    {
        _codeCheckOptions.CodeCheckEnabled = true;
        return Task.CompletedTask;
    }
}