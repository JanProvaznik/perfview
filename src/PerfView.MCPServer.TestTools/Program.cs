using Microsoft.Extensions.Logging;
using PerfView.MCPServer.Tools;
using System;
using System.Text.Json;

namespace PerfView.MCPServer.TestTools;

/// <summary>
/// Simple test harness to demonstrate PerfView MCP Server tool functionality.
/// This shows how tools can be invoked and their responses.
/// </summary>
public class TestTools
{
    public static void Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<TestTools>();

        Console.WriteLine("==============================================");
        Console.WriteLine("PerfView MCP Server - Tool Testing");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        var tools = new PerfViewTools(logger);

        // Test collect_cpu_trace
        Console.WriteLine("1. Testing collect_cpu_trace:");
        Console.WriteLine("   Parameters: duration_seconds=30, output_path=./trace.etl");
        var cpuTraceParams = JsonDocument.Parse(@"{
            ""duration_seconds"": 30,
            ""output_path"": ""./trace.etl"",
            ""process_filter"": ""myapp.exe""
        }").RootElement;
        var cpuTraceResult = tools.CollectCpuTrace(cpuTraceParams);
        Console.WriteLine("   Result:");
        Console.WriteLine(cpuTraceResult);
        Console.WriteLine();

        // Test analyze_cpu_hotspots
        Console.WriteLine("2. Testing analyze_cpu_hotspots:");
        Console.WriteLine("   Parameters: trace_path=./trace.etl, top_n=5");
        var hotspotsParams = JsonDocument.Parse(@"{
            ""trace_path"": ""./trace.etl"",
            ""top_n"": 5
        }").RootElement;
        var hotspotsResult = tools.AnalyzeCpuHotspots(hotspotsParams);
        Console.WriteLine("   Result:");
        Console.WriteLine(hotspotsResult);
        Console.WriteLine();

        // Test collect_memory_trace
        Console.WriteLine("3. Testing collect_memory_trace:");
        Console.WriteLine("   Parameters: process_name=myapp.exe, output_path=./heap.gcdump");
        var memoryParams = JsonDocument.Parse(@"{
            ""process_name"": ""myapp.exe"",
            ""output_path"": ""./heap.gcdump""
        }").RootElement;
        var memoryResult = tools.CollectMemoryTrace(memoryParams);
        Console.WriteLine("   Result:");
        Console.WriteLine(memoryResult);
        Console.WriteLine();

        // Test analyze_memory_growth
        Console.WriteLine("4. Testing analyze_memory_growth:");
        Console.WriteLine("   Parameters: snapshot_path=./heap.gcdump");
        var memGrowthParams = JsonDocument.Parse(@"{
            ""snapshot_path"": ""./heap.gcdump""
        }").RootElement;
        var memGrowthResult = tools.AnalyzeMemoryGrowth(memGrowthParams);
        Console.WriteLine("   Result:");
        Console.WriteLine(memGrowthResult);
        Console.WriteLine();

        // Test diff_traces
        Console.WriteLine("5. Testing diff_traces:");
        Console.WriteLine("   Parameters: baseline_trace=./baseline.etl, comparison_trace=./current.etl");
        var diffParams = JsonDocument.Parse(@"{
            ""baseline_trace"": ""./baseline.etl"",
            ""comparison_trace"": ""./current.etl"",
            ""metric"": ""cpu""
        }").RootElement;
        var diffResult = tools.DiffTraces(diffParams);
        Console.WriteLine("   Result:");
        Console.WriteLine(diffResult);
        Console.WriteLine();

        Console.WriteLine("==============================================");
        Console.WriteLine("All tools executed successfully!");
        Console.WriteLine("==============================================");
    }
}
