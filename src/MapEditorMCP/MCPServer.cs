using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Globalization;
using MapEditorMCP.Scripting;

namespace MapEditorMCP;

public sealed class MCPServer : IDisposable
{
    public const string MCPPath = "/mcp";

    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5.0);

    public MCPServer(WindowManager windowManager, Map map, MutationManager mutationManager, IMutationTarget mutationTarget,
        IMapScreenCropper mapScreenCropper, int port)
    {
        this.windowManager = windowManager;
        mapFacade = new MapFacade(map, mutationManager, mutationTarget);
        scriptingFacade = new ScriptingFacade(map);
        this.mapScreenCropper = mapScreenCropper;
        this.port = port;
    }

    public string ServerURL { get; private set; }

    private readonly WindowManager windowManager;
    private readonly MapFacade mapFacade;
    private readonly ScriptingFacade scriptingFacade;
    private readonly IMapScreenCropper mapScreenCropper;
    private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();
    private readonly int port;

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

        ServerURL = $"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}";

        builder.WebHost.UseUrls(ServerURL);
        builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1;[::1]";
        builder.Logging.AddProvider(new MapEditorMcpLogger());

        builder.Services.AddSingleton(mapFacade);
        builder.Services.AddSingleton(scriptingFacade);
        builder.Services.AddSingleton(mapScreenCropper);
        builder.Services.AddSingleton(new GameThreadDispatcher(windowManager, shutdownCancellationTokenSource.Token));
        builder.Services
            .AddMcpServer(options => options.Filters.Request.CallToolFilters.Add(next => (request, requestCancellationToken) =>
                InvokeToolWithDetailedErrors(next, request, requestCancellationToken)))
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<MapTools>()
            .WithTools<ScriptingTools>();

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

    private static async ValueTask<CallToolResult> InvokeToolWithDetailedErrors(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not McpException &&
                                   ex is not McpProtocolException &&
                                   ex is not InputRequiredException &&
                                   ex is not OperationCanceledException)
        {
            throw new McpException(ex.Message, ex);
        }
    }

    private sealed class MapEditorMcpLogger : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;

        public void Dispose()
        {
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string exceptionDetails = exception == null ? string.Empty : Environment.NewLine + exception;
            Logger.Log($"MCP server {logLevel}: {formatter(state, exception)}{exceptionDetails}");
        }
    }
}
