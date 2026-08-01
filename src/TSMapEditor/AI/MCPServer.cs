using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Rampastring.XNAUI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSMapEditor.AI;

public sealed class MCPServer : IDisposable
{
    public const string ServerUrl = "http://127.0.0.1:32123";
    public const string MCPPath = "/mcp";

    public MCPServer(WindowManager windowManager, MapFacade mapFacade)
    {
        this.windowManager = windowManager;
        this.mapFacade = mapFacade;
    }

    private readonly WindowManager windowManager;
    private readonly MapFacade mapFacade;
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

        if (application != null)
        {
            application.StopAsync().GetAwaiter().GetResult();
            application.DisposeAsync().AsTask().GetAwaiter().GetResult();
            application = null;
        }

        shutdownCancellationTokenSource.Dispose();
    }
}
