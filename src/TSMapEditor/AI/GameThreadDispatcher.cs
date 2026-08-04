using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSMapEditor.AI;

/// <summary>
/// Schedules operations from MCP request threads to run on the game's main thread.
/// </summary>
public sealed class GameThreadDispatcher
{
    public GameThreadDispatcher(WindowManager windowManager, CancellationToken shutdownCancellationToken)
    {
        this.windowManager = windowManager;
        this.shutdownCancellationToken = shutdownCancellationToken;
    }

    private readonly WindowManager windowManager;
    private readonly CancellationToken shutdownCancellationToken;

    public async Task<T> InvokeAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            shutdownCancellationToken);
        CancellationToken linkedCancellationToken = linkedCancellationTokenSource.Token;

        var taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration cancellationTokenRegistration = linkedCancellationToken.Register(
            () => taskCompletionSource.TrySetCanceled(linkedCancellationToken));

        try
        {
            windowManager.AddCallback(new Action(() =>
            {
                if (linkedCancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetCanceled(linkedCancellationToken);
                    return;
                }

                try
                {
                    taskCompletionSource.TrySetResult(operation());
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            taskCompletionSource.TrySetException(ex);
        }

        return await taskCompletionSource.Task.ConfigureAwait(false);
    }
}
