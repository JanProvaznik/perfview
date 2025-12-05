# PerfView MCP Server - Usage Guide

## Quick Start

The PerfView MCP Server is now fully functional and can be connected to AI assistants via the Model Context Protocol (MCP).

### Starting the Server

```bash
cd src/PerfView.MCPServer
dotnet run
```

The server will start and wait for MCP client connections via stdin/stdout.

### Configuring with Claude Desktop

Add the following to your Claude Desktop configuration file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`  
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`  
**Linux**: `~/.config/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "perfview": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/perfview/src/PerfView.MCPServer/PerfView.MCPServer.csproj"
      ]
    }
  }
}
```

## Available Tools

### 1. collect_cpu_trace

Collects a CPU sampling trace for performance analysis.

**Parameters:**
- `durationSeconds` (int, default: 30): Duration in seconds to collect the trace
- `outputPath` (string, default: "./trace.etl"): Output file path for the trace
- `processFilter` (string, optional): Optional process name filter

**Example:**
```
User: Collect a 30-second CPU trace and save it to ./myapp_trace.etl

Claude: [Calls collect_cpu_trace tool with parameters]

Result:
CPU trace collected successfully.
File: /path/to/myapp_trace.etl
Size: 45.32 MB
Duration: 30s

Use 'analyze_cpu_hotspots' to analyze the trace.
```

**Note**: Requires administrator privileges on Windows for kernel-mode tracing.

### 2. analyze_cpu_hotspots

Analyzes a CPU trace file to identify performance hotspots.

**Parameters:**
- `tracePath` (string, required): Path to the ETL trace file to analyze
- `topN` (int, default: 10): Number of top methods to return
- `processFilter` (string, optional): Optional process name filter

**Example:**
```
User: Analyze the CPU hotspots in myapp_trace.etl and show me the top 10 methods

Claude: [Calls analyze_cpu_hotspots tool]

Result:
CPU Hotspot Analysis: myapp_trace.etl
============================================================

Total CPU Samples: 15,432

Top 10 CPU-Consuming Methods:
------------------------------------------------------------

 1.  25.43% (  3,925 samples) - 0x7FF123456789
 2.  18.21% (  2,810 samples) - 0x7FF987654321
 3.  12.15% (  1,875 samples) - 0x7FF111222333
...
```

### 3. query_events

Queries and filters ETW events from a trace file.

**Parameters:**
- `tracePath` (string, required): Path to the ETL trace file
- `providerName` (string, optional): ETW provider name filter
- `eventName` (string, optional): Event name filter
- `limit` (int, default: 100): Maximum number of events to return

**Example:**
```
User: Show me process start events from the trace

Claude: [Calls query_events with providerName="Microsoft-Windows-Kernel-Process", eventName="ProcessStart"]

Result:
Event Query Results: myapp_trace.etl
================================================================================

Provider Filter: Microsoft-Windows-Kernel-Process
Event Filter: ProcessStart
Limit: 100

--------------------------------------------------------------------------------

[10:25:34.123] Microsoft-Windows-Kernel-Process/ProcessStart
  Process: myapp.exe (PID: 1234)
  Payload: ImageFileName=myapp.exe, ProcessID=1234, ParentPID=5678

[10:25:35.456] Microsoft-Windows-Kernel-Process/ProcessStart
  Process: child.exe (PID: 9012)
  Payload: ImageFileName=child.exe, ProcessID=9012, ParentPID=1234
...
```

### 4. get_trace_stats

Gets high-level statistics about a trace file.

**Parameters:**
- `tracePath` (string, required): Path to the ETL trace file

**Example:**
```
User: Give me statistics about myapp_trace.etl

Claude: [Calls get_trace_stats tool]

Result:
Trace Statistics: myapp_trace.etl
============================================================

File Size: 45.32 MB
Created: 2024-12-05 10:25:30

Duration: 30.15 seconds
Start Time: 2024-12-05 10:25:30.123
End Time: 2024-12-05 10:26:00.273

Total Events: 1,234,567
Unique Providers: 45
Unique Processes: 12

Top 5 Event Providers:
------------------------------------------------------------
     500,123 events - Microsoft-Windows-Kernel-Process
     250,456 events - Microsoft-Windows-DotNETRuntime
     125,789 events - Microsoft-Windows-Kernel-File
...
```

## Example Workflow

### Scenario: Investigate Application Performance

```
User: I need to investigate why my application is running slowly. 
      Can you help me collect a trace and analyze it?

Claude: I'll help you investigate the performance issue. First, let me 
        collect a CPU trace of your application. What's the process name?

User: The process is MyApp.exe

Claude: [Calls collect_cpu_trace with processFilter="MyApp.exe", durationSeconds=30]

        I've collected a 30-second CPU trace. Now let me analyze it to 
        identify the performance hotspots.

        [Calls analyze_cpu_hotspots on the collected trace]

        Based on the analysis, here are the top CPU consumers:
        
        1. 35.2% - 0x7FF123456789 (likely in your main processing loop)
        2. 18.5% - 0x7FF987654321 (related to database operations)
        3. 12.3% - 0x7FF111222333 (JSON serialization)
        
        The main bottleneck appears to be in your processing loop, which 
        is consuming over 35% of CPU time. Would you like me to:
        1. Collect a more detailed trace
        2. Analyze specific events to understand what's happening
        3. Look at other aspects of performance

User: Can you show me what events are happening during the slowness?

Claude: [Calls query_events with appropriate filters]

        I can see several patterns:
        - Frequent file I/O operations
        - Database query events every few milliseconds
        - GC events indicating memory pressure
        
        The performance issue seems to stem from excessive I/O operations
        combined with memory allocations causing frequent garbage collections.
        
        Recommendations:
        1. Batch your file operations
        2. Cache database queries where possible
        3. Reduce object allocations in hot paths
```

## Prerequisites

### Windows
- Administrator privileges (for kernel-mode tracing)
- .NET 8.0 Runtime
- Windows 10 or later

### Linux
Note: Full kernel tracing is Windows-only, but EventPipe traces from .NET applications can be analyzed on Linux.

## Troubleshooting

### "Administrator privileges required"

CPU trace collection requires admin rights on Windows. Run the server with elevated permissions:
- Right-click command prompt/terminal and select "Run as Administrator"
- Or configure Claude Desktop to run with elevated permissions

### "Trace file not found"

Make sure the trace file path is correct and accessible. Use absolute paths for clarity.

### Server doesn't start

Check that:
- .NET 8.0 SDK is installed: `dotnet --version`
- All dependencies are restored: `dotnet restore`
- Project builds successfully: `dotnet build`

### No CPU samples in analysis

This can happen if:
- The trace duration was too short
- The process wasn't active during tracing
- Symbols aren't resolved (addresses shown instead of method names)

## Symbol Resolution

For better results with method names instead of addresses, ensure:
1. PDB symbol files are available
2. Symbol paths are configured
3. Using Debug builds for your application during analysis

Future versions will include automatic symbol resolution.

## Next Steps

After analyzing traces, you can:
1. Collect more targeted traces with specific process filters
2. Query specific events to understand behavior
3. Use the results to optimize your application
4. Share trace files for deeper analysis

## Advanced Usage

### Programmatic Access

The MCP server can be accessed programmatically using any MCP client library. The protocol uses JSON-RPC 2.0 over stdin/stdout.

Example initialize request:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {"name": "my-client", "version": "1.0"}
  }
}
```

Example tool call:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_trace_stats",
    "arguments": {
      "tracePath": "./trace.etl"
    }
  }
}
```

## Support

For issues or questions:
- GitHub Issues: https://github.com/microsoft/perfview/issues
- Documentation: See PerfViewMCPServer.md for full specification
