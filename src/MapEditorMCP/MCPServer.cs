using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorMCP.Http;
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
        : this(windowManager, map, mutationManager, mutationTarget, mapScreenCropper, null, null, port)
    {
    }

    public MCPServer(WindowManager windowManager, Map map, MutationManager mutationManager, IMutationTarget mutationTarget,
        IMapScreenCropper mapScreenCropper, IEditorViewController viewController, IMapSaveHandler saveHandler, int port)
    {
        this.windowManager = windowManager;
        hostServices = new EditorHostServices(viewController, saveHandler);
        mapFacade = new MapFacade(map, mutationManager, mutationTarget);
        scriptingFacade = new ScriptingFacade(map, mutationManager, mutationTarget);
        this.mapScreenCropper = mapScreenCropper;
        this.port = port;
    }

    public string ServerURL { get; private set; }

    private readonly WindowManager windowManager;
    private readonly MapFacade mapFacade;
    private readonly ScriptingFacade scriptingFacade;
    private readonly IMapScreenCropper mapScreenCropper;
    private readonly EditorHostServices hostServices;
    private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();
    private readonly int port;

    private ServiceProvider serviceProvider;
    private LoopbackHttpServer httpServer;
    private bool disposed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (httpServer != null)
            return;

        var loggerFactory = new MapEditorMcpLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(mapFacade);
        services.AddSingleton(scriptingFacade);
        services.AddSingleton(mapScreenCropper);
        services.AddSingleton(hostServices);
        services.AddSingleton(new GameThreadDispatcher(windowManager, shutdownCancellationTokenSource.Token));
        services
            .AddMcpServer(options => options.Filters.Request.CallToolFilters.Add(next => (request, requestCancellationToken) =>
                InvokeToolWithDetailedErrors(next, request, requestCancellationToken)))
            .WithTools<MapTools>()
            .WithTools<ScriptingTools>()
            .WithTools<EditorTools>();

        serviceProvider = services.BuildServiceProvider();
        httpServer = new LoopbackHttpServer(port, boundPort =>
        {
            ServerURL = $"http://127.0.0.1:{boundPort.ToString(CultureInfo.InvariantCulture)}";
            var endpoint = new McpHttpEndpoint(serviceProvider, loggerFactory, boundPort, MCPPath);
            return endpoint.HandleRequestAsync;
        }, cancelRequestOnReadEof: true);
        await httpServer.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        shutdownCancellationTokenSource.Cancel();
        mapScreenCropper.StopScreenCropRequests();

        LoopbackHttpServer httpServerToDispose = httpServer;
        ServiceProvider serviceProviderToDispose = serviceProvider;
        httpServer = null;
        serviceProvider = null;

        if (httpServerToDispose == null)
        {
            serviceProviderToDispose?.Dispose();
            shutdownCancellationTokenSource.Dispose();
            return;
        }

        _ = Task.Run(() => StopAndDisposeAsync(httpServerToDispose, serviceProviderToDispose));
    }

    private async Task StopAndDisposeAsync(LoopbackHttpServer httpServerToDispose, ServiceProvider serviceProviderToDispose)
    {
        Task cleanupTask = DisposeServerResourcesAsync(httpServerToDispose, serviceProviderToDispose);
        try
        {
            await cleanupTask.WaitAsync(ShutdownTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"MCP server cleanup is still running after {ShutdownTimeout.TotalSeconds} seconds.");
            _ = cleanupTask.ContinueWith(
                task => Logger.Log("MCP server cleanup eventually failed. Returned error: " + task.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to clean up the MCP server. Returned error: " + ex.Message);
        }
    }

    private async Task DisposeServerResourcesAsync(
        LoopbackHttpServer httpServerToDispose,
        ServiceProvider serviceProviderToDispose)
    {
        try
        {
            await httpServerToDispose.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                serviceProviderToDispose?.Dispose();
            }
            finally
            {
                shutdownCancellationTokenSource.Dispose();
            }
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

    private sealed class MapEditorMcpLogger : ILoggerFactory, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;

        public void AddProvider(ILoggerProvider provider)
        {
        }

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
