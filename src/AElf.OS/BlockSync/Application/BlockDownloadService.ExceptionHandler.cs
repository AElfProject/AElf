using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.BlockSync.Events;
using AElf.OS.BlockSync.Exceptions;

namespace AElf.OS.BlockSync.Application;

public partial class BlockDownloadService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileDownloadingBlocks(
        BlockDownloadException ex)
    {
        await LocalEventBus.PublishAsync(new AbnormalPeerFoundEventData
        {
            BlockHash = ex.BlockHash,
            BlockHeight = ex.BlockHeight,
            PeerPubkey = ex.PeerPubkey
        });
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue,
        };
    }
}