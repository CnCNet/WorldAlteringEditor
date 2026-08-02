using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Threading;
using System.Threading.Tasks;
using TSMapEditor.Rendering;

namespace TSMapEditor.AI;

public sealed class MCPServer : IDisposable
{
    public const string ServerUrl = "http://127.0.0.1:32123";
    public const string MCPPath = "/mcp";

    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5.0);

    public MCPServer(WindowManager windowManager, MapFacade mapFacade, IMapScreenCropper mapScreenCropper)
    {
        this.windowManager = windowManager;
        this.mapFacade = mapFacade;
        this.mapScreenCropper = mapScreenCropper;
    }

    private readonly WindowManager windowManager;
    private readonly MapFacade mapFacade;
    private readonly IMapScreenCropper mapScreenCropper;
    private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();

    private WebApplication application;
    private bool disposed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (application != null)
            return;

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ApplicationName = typeof(MCPServer).Assembly.FullName
        });

        builder.WebHost.UseUrls(ServerUrl);
        builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1;[::1]";

        builder.Services.AddSingleton(mapFacade);
        builder.Services.AddSingleton(mapScreenCropper);
        builder.Services.AddSingleton(new GameThreadDispatcher(windowManager, shutdownCancellationTokenSource.Token));
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<MapTools>();

        application = builder.Build();
        application.MapMcp(MCPPath);

        await application.StartAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        shutdownCancellationTokenSource.Cancel();
        mapScreenCropper.StopScreenCropRequests();

        WebApplication applicationToDispose = application;
        application = null;

        if (applicationToDispose == null)
        {
            shutdownCancellationTokenSource.Dispose();
            return;
        }

        _ = Task.Run(() => StopAndDisposeAsync(applicationToDispose));
    }

    private async Task StopAndDisposeAsync(WebApplication applicationToDispose)
    {
        try
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(ShutdownTimeout);
            await applicationToDispose.StopAsync(timeoutCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Logger.Log($"MCP server did not stop gracefully within {ShutdownTimeout.TotalSeconds} seconds.");
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to stop the MCP server cleanly. Returned error: " + ex.Message);
        }

        try
        {
            await applicationToDispose.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to dispose the MCP server cleanly. Returned error: " + ex.Message);
        }
        finally
        {
            shutdownCancellationTokenSource.Dispose();
        }
    }
}
