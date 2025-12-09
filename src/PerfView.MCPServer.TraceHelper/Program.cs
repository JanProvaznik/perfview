using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace PerfView.MCPServer.TraceHelper;

/// <summary>
/// Elevated helper process for trace collection operations.
/// This process can be spawned with UAC elevation to perform privileged operations.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: PerfView.MCPServer.TraceHelper <command> [options]");
                return 1;
            }

            var command = args[0].ToLowerInvariant();

            return command switch
            {
                "collect-cpu" => CollectCpuTrace(args),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static int CollectCpuTrace(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: collect-cpu <duration_seconds> <output_path> [process_filter] [result_file_path]");
            return 1;
        }

        if (!int.TryParse(args[1], out int durationSeconds) || durationSeconds <= 0)
        {
            Console.Error.WriteLine("ERROR: Invalid duration");
            return 1;
        }

        string outputPath = args[2];
        string? processFilter = args.Length > 3 && !args[3].EndsWith(".json") ? args[3] : null;
        string? resultFilePath = args.Length > 3 ? args[^1] : null; // Last argument might be result file
        
        // Check if last arg is a result file path (ends with .json)
        if (resultFilePath != null && !resultFilePath.EndsWith(".json"))
        {
            resultFilePath = null; // Not a result file path
        }

        try
        {
            var absolutePath = Path.GetFullPath(outputPath);
            var sessionName = $"PerfView_MCP_Helper_{Guid.NewGuid():N}";

            Console.WriteLine($"Starting trace collection: {durationSeconds}s -> {absolutePath}");

            using (var session = new TraceEventSession(sessionName, absolutePath))
            {
                session.StopOnDispose = true;

                // Enable kernel providers for CPU sampling
                session.EnableKernelProvider(
                    KernelTraceEventParser.Keywords.Profile |
                    KernelTraceEventParser.Keywords.Process |
                    KernelTraceEventParser.Keywords.Thread |
                    KernelTraceEventParser.Keywords.ImageLoad);

                Console.WriteLine("Trace session started, collecting...");

                // Collect for specified duration
                Thread.Sleep(TimeSpan.FromSeconds(durationSeconds));

                Console.WriteLine("Stopping trace session...");
            }

            var fileInfo = new FileInfo(absolutePath);
            var result = new
            {
                success = true,
                file_path = absolutePath,
                file_size_mb = fileInfo.Length / 1024.0 / 1024.0,
                duration_seconds = durationSeconds,
                process_filter = processFilter
            };

            var resultJson = JsonSerializer.Serialize(result);
            
            // Write result to file if path provided (Windows UAC scenario)
            if (resultFilePath != null)
            {
                try
                {
                    File.WriteAllText(resultFilePath, resultJson);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Could not write result file: {ex.Message}");
                }
            }
            
            // Also output to stdout for non-UAC scenarios
            Console.WriteLine("RESULT:" + resultJson);
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            var errorResult = new
            {
                success = false,
                error = "Still insufficient privileges. Run as Administrator.",
                error_type = "UnauthorizedAccess"
            };
            
            WriteErrorResult(errorResult, resultFilePath);
            return 1;
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                success = false,
                error = ex.Message,
                error_type = ex.GetType().Name
            };
            
            WriteErrorResult(errorResult, resultFilePath);
            return 1;
        }
    }
    
    static void WriteErrorResult(object errorResult, string? resultFilePath)
    {
        var errorJson = JsonSerializer.Serialize(errorResult);
        
        // Write to result file if provided
        if (resultFilePath != null)
        {
            try
            {
                File.WriteAllText(resultFilePath, errorJson);
            }
            catch
            {
                // Ignore errors writing error file
            }
        }
        
        // Also write to stderr
        Console.Error.WriteLine($"ERROR: {errorJson}");
    }

    static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"ERROR: Unknown command '{command}'");
        Console.Error.WriteLine("Available commands:");
        Console.Error.WriteLine("  collect-cpu <duration> <output_path> [process_filter]");
        return 1;
    }
}
