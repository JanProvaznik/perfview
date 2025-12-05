using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
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
        
        await builder.Build().RunAsync();
    }
}
