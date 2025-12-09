using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PerfView.MCPServer.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PerfView.MCPServer.Tools;

/// <summary>
/// MCP tools for trace collection and analysis using TraceEvent library.
/// </summary>
[McpServerToolType]
public class TraceTools
{
    private static readonly ConcurrentDictionary<string, TraceEventSession> _activeSessions = new();

    [McpServerTool(Name = "collect_cpu_trace")]
    [Description("Collects a CPU sampling trace for performance analysis. Automatically requests elevation if needed.")]
    public static async Task<string> CollectCpuTrace(
        ILogger<TraceTools> logger,
        [Description("Duration in seconds to collect the trace")] int durationSeconds = 30,
        [Description("Output file path for the trace (e.g., ./trace.etl)")] string outputPath = "./trace.etl",
        [Description("Optional process name filter (e.g., myapp.exe)")] string? processFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting CPU trace collection for {Duration}s to {Path}", durationSeconds, outputPath);

        var absolutePath = Path.GetFullPath(outputPath);

        // Check for administrator privileges and use elevated helper if needed
        if (!PrivilegeElevation.IsAdministrator())
        {
            logger.LogWarning("CPU trace collection requires administrator privileges, spawning elevated helper...");
            
            try
            {
                var args = new List<string>
                {
                    durationSeconds.ToString(),
                    absolutePath
                };
                
                if (!string.IsNullOrEmpty(processFilter))
                {
                    args.Add(processFilter);
                }

                var (exitCode, stdout, stderr) = await PrivilegeElevation.ExecuteElevatedAsync(
                    "collect-cpu", args.ToArray());

                if (exitCode != 0)
                {
                    // Try to parse error from result JSON
                    string errorMessage = "Failed to collect trace with elevation";
                    
                    if (!string.IsNullOrEmpty(stdout))
                    {
                        try
                        {
                            var errorResult = JsonDocument.Parse(stdout);
                            if (errorResult.RootElement.TryGetProperty("success", out var successProp) && 
                                !successProp.GetBoolean() &&
                                errorResult.RootElement.TryGetProperty("error", out var errorProp))
                            {
                                errorMessage = errorProp.GetString() ?? errorMessage;
                            }
                        }
                        catch
                        {
                            // Not JSON, use as-is if not empty
                            if (!string.IsNullOrWhiteSpace(stdout))
                            {
                                errorMessage = stdout;
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        errorMessage += $"\n{stderr}";
                    }
                    
                    logger.LogError("Elevated trace collection failed: {Error}", errorMessage);
                    return $"ERROR: {errorMessage}\n\nIf UAC prompt was denied, approve it to allow trace collection.";
                }

                // Parse result from stdout (could be JSON directly or RESULT: prefixed line)
                string resultJson = stdout;
                var resultLine = stdout.Split('\n').FirstOrDefault(l => l.StartsWith("RESULT:"));
                if (resultLine != null)
                {
                    resultJson = resultLine.Substring("RESULT:".Length);
                }

                try
                {
                    var result = JsonDocument.Parse(resultJson);
                    var root = result.RootElement;
                    
                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        var fileSizeMb = root.GetProperty("file_size_mb").GetDouble();
                        return $"CPU trace collected successfully (with elevation).\nFile: {absolutePath}\nSize: {fileSizeMb:F2} MB\nDuration: {durationSeconds}s\n\nUse 'analyze_cpu_hotspots' to analyze the trace.";
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning("Could not parse helper result JSON: {Error}", ex.Message);
                }

                // Fallback: check if file exists
                var fileInfo = new FileInfo(absolutePath);
                if (fileInfo.Exists)
                {
                    return $"CPU trace collected successfully (with elevation).\nFile: {absolutePath}\nSize: {fileInfo.Length / 1024.0 / 1024.0:F2} MB\nDuration: {durationSeconds}s\n\nUse 'analyze_cpu_hotspots' to analyze the trace.";
                }

                return "Trace collection completed but unable to verify output file.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute elevated trace collection");
                return $"ERROR: Failed to spawn elevated helper: {ex.Message}\n\nMake sure PerfView.MCPServer.TraceHelper is built and accessible.";
            }
        }

        // We have privileges, collect directly
        TraceEventSession? session = null;
        var sessionName = $"PerfView_MCP_{Guid.NewGuid():N}";
        
        try
        {

            // Create trace session
            session = new TraceEventSession(sessionName, absolutePath)
            {
                StopOnDispose = true
            };

            // Enable kernel providers for CPU sampling
            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.Profile | 
                KernelTraceEventParser.Keywords.Process | 
                KernelTraceEventParser.Keywords.Thread | 
                KernelTraceEventParser.Keywords.ImageLoad);

            // Track active session
            _activeSessions.TryAdd(sessionName, session);

            // Collect for specified duration
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);

            // Stop the session
            session.Stop();

            var fileInfo = new FileInfo(absolutePath);
            logger.LogInformation("CPU trace collected successfully: {Size} MB", fileInfo.Length / 1024.0 / 1024.0);

            return $"CPU trace collected successfully.\nFile: {absolutePath}\nSize: {fileInfo.Length / 1024.0 / 1024.0:F2} MB\nDuration: {durationSeconds}s\n\nUse 'analyze_cpu_hotspots' to analyze the trace.";
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogError("Failed to collect trace: Administrator privileges required despite check");
            return "ERROR: Insufficient privileges for trace collection despite elevation check. This may indicate a permission issue with the trace file location.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect CPU trace");
            return $"ERROR: Failed to collect CPU trace: {ex.Message}";
        }
        finally
        {
            // Ensure session is removed from tracking and disposed
            if (session != null)
            {
                _activeSessions.TryRemove(sessionName, out _);
                session.Dispose();
            }
        }
    }

    [McpServerTool(Name = "analyze_cpu_hotspots")]
    [Description("Analyzes a CPU trace file to identify performance hotspots and time-consuming methods.")]
    public static async Task<string> AnalyzeCpuHotspots(
        ILogger<TraceTools> logger,
        [Description("Path to the ETL trace file to analyze")] string tracePath,
        [Description("Number of top methods to return")] int topN = 10,
        [Description("Optional process name filter")] string? processFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Analyzing CPU hotspots in {Path}", tracePath);

        try
        {
            var absolutePath = Path.GetFullPath(tracePath);
            if (!File.Exists(absolutePath))
            {
                return $"ERROR: Trace file not found: {absolutePath}";
            }

            var result = new StringBuilder();
            result.AppendLine($"CPU Hotspot Analysis: {Path.GetFileName(absolutePath)}");
            result.AppendLine(new string('=', 60));
            result.AppendLine();

            // Dictionary to accumulate stack samples by method
            var methodSamples = new Dictionary<string, int>();
            var totalSamples = 0;

            await Task.Run(() =>
            {
                using var source = new ETWTraceEventSource(absolutePath);
                
                // Track processes
                var processNames = new Dictionary<int, string>();
                source.Kernel.ProcessStart += data =>
                {
                    processNames[data.ProcessID] = data.ProcessName;
                };

                // Collect CPU samples
                source.Kernel.PerfInfoSample += data =>
                {
                    if (processFilter != null && processNames.TryGetValue(data.ProcessID, out var pname))
                    {
                        if (!pname.Equals(processFilter, StringComparison.OrdinalIgnoreCase))
                            return;
                    }

                    totalSamples++;
                    
                    // Track instruction pointer
                    var ip = data.InstructionPointer;
                    var frame = $"0x{ip:X}";
                    
                    if (methodSamples.ContainsKey(frame))
                        methodSamples[frame]++;
                    else
                        methodSamples[frame] = 1;
                };

                source.Process();
            }, cancellationToken);

            if (totalSamples == 0)
            {
                return "No CPU samples found in trace. The trace may be empty or corrupted.";
            }

            result.AppendLine($"Total CPU Samples: {totalSamples:N0}");
            if (processFilter != null)
            {
                result.AppendLine($"Process Filter: {processFilter}");
            }
            result.AppendLine();
            result.AppendLine($"Top {topN} CPU-Consuming Methods:");
            result.AppendLine(new string('-', 60));
            result.AppendLine();

            var topMethods = methodSamples
                .OrderByDescending(kvp => kvp.Value)
                .Take(topN);

            var rank = 1;
            foreach (var method in topMethods)
            {
                var percentage = (method.Value * 100.0) / totalSamples;
                result.AppendLine($"{rank,2}. {percentage,6:F2}% ({method.Value,8:N0} samples) - {method.Key}");
                rank++;
            }

            logger.LogInformation("Analysis complete: {TotalSamples} samples, {UniqueFrames} unique methods", 
                totalSamples, methodSamples.Count);

            return result.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze CPU hotspots");
            return $"ERROR: Failed to analyze CPU hotspots: {ex.Message}";
        }
    }

    [McpServerTool(Name = "query_events")]
    [Description("Queries and filters ETW events from a trace file.")]
    public static async Task<string> QueryEvents(
        ILogger<TraceTools> logger,
        [Description("Path to the ETL trace file")] string tracePath,
        [Description("Optional ETW provider name filter (e.g., 'Microsoft-Windows-Kernel-Process')")] string? providerName = null,
        [Description("Optional event name filter (e.g., 'ProcessStart')")] string? eventName = null,
        [Description("Maximum number of events to return")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Querying events from {Path}", tracePath);

        try
        {
            var absolutePath = Path.GetFullPath(tracePath);
            if (!File.Exists(absolutePath))
            {
                return $"ERROR: Trace file not found: {absolutePath}";
            }

            var result = new StringBuilder();
            result.AppendLine($"Event Query Results: {Path.GetFileName(absolutePath)}");
            result.AppendLine(new string('=', 80));
            result.AppendLine();

            if (providerName != null)
                result.AppendLine($"Provider Filter: {providerName}");
            if (eventName != null)
                result.AppendLine($"Event Filter: {eventName}");
            result.AppendLine($"Limit: {limit}");
            result.AppendLine();
            result.AppendLine(new string('-', 80));
            result.AppendLine();

            var eventCount = 0;
            var matchedEvents = 0;

            await Task.Run(() =>
            {
                using var source = new ETWTraceEventSource(absolutePath);
                
                source.Dynamic.All += data =>
                {
                    if (matchedEvents >= limit)
                        return;

                    eventCount++;

                    // Apply filters
                    if (providerName != null && !data.ProviderName.Contains(providerName, StringComparison.OrdinalIgnoreCase))
                        return;

                    if (eventName != null && !data.EventName.Contains(eventName, StringComparison.OrdinalIgnoreCase))
                        return;

                    matchedEvents++;

                    result.AppendLine($"[{data.TimeStamp:HH:mm:ss.fff}] {data.ProviderName}/{data.EventName}");
                    result.AppendLine($"  Process: {data.ProcessName} (PID: {data.ProcessID})");
                    
                    // Show first few payload fields
                    var payloadNames = data.PayloadNames;
                    if (payloadNames.Length > 0)
                    {
                        result.Append("  Payload: ");
                        for (int i = 0; i < Math.Min(3, payloadNames.Length); i++)
                        {
                            if (i > 0) result.Append(", ");
                            result.Append($"{payloadNames[i]}={data.PayloadValue(i)}");
                        }
                        if (payloadNames.Length > 3)
                            result.Append($" ... ({payloadNames.Length - 3} more fields)");
                        result.AppendLine();
                    }
                    result.AppendLine();
                };

                source.Process();
            }, cancellationToken);

            result.AppendLine(new string('-', 80));
            result.AppendLine($"Total events processed: {eventCount:N0}");
            result.AppendLine($"Matched events: {matchedEvents:N0}");

            if (matchedEvents == 0)
            {
                result.AppendLine();
                result.AppendLine("No events matched the specified filters.");
            }
            else if (matchedEvents >= limit)
            {
                result.AppendLine();
                result.AppendLine($"Note: Output limited to {limit} events. Increase 'limit' parameter to see more.");
            }

            logger.LogInformation("Query complete: {Matched}/{Total} events", matchedEvents, eventCount);

            return result.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query events");
            return $"ERROR: Failed to query events: {ex.Message}";
        }
    }

    [McpServerTool(Name = "get_trace_stats")]
    [Description("Gets high-level statistics about a trace file.")]
    public static async Task<string> GetTraceStats(
        ILogger<TraceTools> logger,
        [Description("Path to the ETL trace file")] string tracePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting trace stats for {Path}", tracePath);

        try
        {
            var absolutePath = Path.GetFullPath(tracePath);
            if (!File.Exists(absolutePath))
            {
                return $"ERROR: Trace file not found: {absolutePath}";
            }

            var result = new StringBuilder();
            result.AppendLine($"Trace Statistics: {Path.GetFileName(absolutePath)}");
            result.AppendLine(new string('=', 60));
            result.AppendLine();

            var fileInfo = new FileInfo(absolutePath);
            result.AppendLine($"File Size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
            result.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();

            var eventCount = 0;
            var providerCounts = new Dictionary<string, int>();
            var processCounts = new Dictionary<string, int>();
            DateTime? firstEvent = null;
            DateTime? lastEvent = null;

            await Task.Run(() =>
            {
                using var source = new ETWTraceEventSource(absolutePath);
                
                source.Dynamic.All += data =>
                {
                    eventCount++;
                    
                    if (firstEvent == null)
                        firstEvent = data.TimeStamp;
                    lastEvent = data.TimeStamp;

                    var provider = data.ProviderName;
                    if (providerCounts.ContainsKey(provider))
                        providerCounts[provider]++;
                    else
                        providerCounts[provider] = 1;

                    var process = data.ProcessName ?? "Unknown";
                    if (processCounts.ContainsKey(process))
                        processCounts[process]++;
                    else
                        processCounts[process] = 1;
                };

                source.Process();
            }, cancellationToken);

            if (firstEvent != null && lastEvent != null)
            {
                var duration = lastEvent.Value - firstEvent.Value;
                result.AppendLine($"Duration: {duration.TotalSeconds:F2} seconds");
                result.AppendLine($"Start Time: {firstEvent:yyyy-MM-dd HH:mm:ss.fff}");
                result.AppendLine($"End Time: {lastEvent:yyyy-MM-dd HH:mm:ss.fff}");
                result.AppendLine();
            }

            result.AppendLine($"Total Events: {eventCount:N0}");
            result.AppendLine($"Unique Providers: {providerCounts.Count:N0}");
            result.AppendLine($"Unique Processes: {processCounts.Count:N0}");
            result.AppendLine();

            result.AppendLine("Top 5 Event Providers:");
            result.AppendLine(new string('-', 60));
            foreach (var provider in providerCounts.OrderByDescending(p => p.Value).Take(5))
            {
                result.AppendLine($"  {provider.Value,10:N0} events - {provider.Key}");
            }
            result.AppendLine();

            result.AppendLine("Top 5 Processes:");
            result.AppendLine(new string('-', 60));
            foreach (var process in processCounts.OrderByDescending(p => p.Value).Take(5))
            {
                result.AppendLine($"  {process.Value,10:N0} events - {process.Key}");
            }

            logger.LogInformation("Stats complete: {Events} events from {Providers} providers", 
                eventCount, providerCounts.Count);

            return result.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get trace stats");
            return $"ERROR: Failed to get trace stats: {ex.Message}";
        }
    }
}
