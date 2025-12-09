using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PerfView.MCPServer.Utilities;
using System.Threading.Tasks;

namespace PerfView.MCPServer;

/// <summary>
/// Entry point for the PerfView MCP Server.
/// This server exposes PerfView's performance analysis capabilities through the Model Context Protocol,
/// enabling AI assistants to automate performance investigations, trace analysis, and memory profiling.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure logging to stderr (required for MCP protocol)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        
        // Configure MCP server
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(); // This will discover all [McpServerTool] methods

        var host = builder.Build();
        
        // Log privilege status on startup
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        if (PrivilegeElevation.IsAdministrator())
        {
            logger.LogInformation("PerfView MCP Server starting with administrator privileges");
        }
        else
        {
            logger.LogWarning("PerfView MCP Server starting without administrator privileges");
            logger.LogWarning("Trace collection features will require privilege elevation");
        }
        
        await host.RunAsync();
    }
}
