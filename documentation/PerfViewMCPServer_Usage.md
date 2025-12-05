# PerfView MCP Server - Usage Guide

## Overview

The PerfView MCP Server provides AI assistants with programmatic access to PerfView's performance analysis capabilities. This guide explains how to use the server.

## Current Status

✅ **Completed:**
- Comprehensive specification document
- Project structure and build system
- Tool scaffolds for all 12 planned tools
- Test harness demonstrating tool invocation
- Documentation

🚧 **In Progress:**
- Full MCP protocol integration (waiting on API verification)
- TraceEvent library integration for actual trace collection/analysis
- MemoryGraph integration for heap analysis

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Windows OS (for full ETW support)
- Optional: Administrator privileges for kernel-mode tracing

### Building

```bash
cd src/PerfView.MCPServer
dotnet build
```

### Running the Server

```bash
cd src/PerfView.MCPServer
dotnet run
```

The server will start and display available tools:

```
info: PerfView MCP Server - Initial Implementation
info: ============================================
info: 
info: Available tool categories:
info:   - Trace Collection: collect_cpu_trace, collect_memory_trace, stop_trace
info:   - Trace Analysis: analyze_cpu_hotspots, analyze_memory_growth, query_events, get_trace_stats
info:   - Comparison: diff_traces, diff_heaps
info:   - Symbols: resolve_symbols, get_source_location
info:   - Reports: generate_html_report
```

### Running Tests

To test the tool implementations without MCP protocol:

```bash
cd src/PerfView.MCPServer.TestTools
dotnet run
```

This will execute sample calls to each tool and display the JSON responses.

## Tool Reference

### Trace Collection Tools

#### collect_cpu_trace

Collect a CPU sampling trace.

**Input:**
```json
{
  "duration_seconds": 30,
  "output_path": "./trace.etl",
  "process_filter": "myapp.exe"
}
```

**Output:**
```json
{
  "status": "success",
  "message": "CPU trace collection started for 30 seconds",
  "trace_path": "./trace.etl"
}
```

#### collect_memory_trace

Collect a GC heap snapshot.

**Input:**
```json
{
  "process_name": "myapp.exe",
  "output_path": "./heap.gcdump"
}
```

**Output:**
```json
{
  "status": "success",
  "snapshot_path": "./heap.gcdump",
  "heap_size_mb": 150.5
}
```

### Trace Analysis Tools

#### analyze_cpu_hotspots

Identify CPU-intensive methods.

**Input:**
```json
{
  "trace_path": "./trace.etl",
  "top_n": 10
}
```

**Output:**
```json
{
  "status": "success",
  "top_methods": [
    {
      "method": "MyApp.ProcessData",
      "inclusive_ms": 1500.0,
      "exclusive_ms": 800.0,
      "percentage": 35.0
    }
  ]
}
```

#### analyze_memory_growth

Detect memory leaks.

**Input:**
```json
{
  "snapshot_path": "./heap.gcdump",
  "baseline_path": "./baseline.gcdump"
}
```

**Output:**
```json
{
  "status": "success",
  "total_heap_mb": 200.0,
  "growth_suspects": [
    {
      "type": "System.String",
      "count": 15000,
      "total_kb": 7500
    }
  ]
}
```

#### query_events

Query ETW events from a trace.

**Input:**
```json
{
  "trace_path": "./trace.etl",
  "provider_name": "Microsoft-Windows-DotNETRuntime",
  "event_name": "GC/Start",
  "limit": 100
}
```

**Output:**
```json
{
  "status": "success",
  "event_count": 25,
  "events": [
    {
      "timestamp_ms": 1000.0,
      "provider": "Microsoft-Windows-DotNETRuntime",
      "eventName": "GC/Start",
      "payload": {
        "Count": 1,
        "Depth": 0,
        "Reason": "AllocSmall"
      }
    }
  ]
}
```

#### get_trace_stats

Get trace overview.

**Input:**
```json
{
  "trace_path": "./trace.etl"
}
```

**Output:**
```json
{
  "status": "success",
  "duration_seconds": 30.0,
  "file_size_mb": 125.5,
  "process_count": 5,
  "event_count": 1500000
}
```

### Comparison Tools

#### diff_traces

Compare two traces.

**Input:**
```json
{
  "baseline_trace": "./baseline.etl",
  "comparison_trace": "./current.etl",
  "metric": "cpu"
}
```

**Output:**
```json
{
  "status": "success",
  "differences": [
    {
      "method": "MyApp.SlowMethod",
      "baseline_ms": 100.0,
      "comparison_ms": 250.0,
      "delta_percent": 150.0,
      "regression": true
    }
  ]
}
```

#### diff_heaps

Compare memory snapshots.

**Input:**
```json
{
  "baseline_snapshot": "./baseline.gcdump",
  "comparison_snapshot": "./current.gcdump"
}
```

**Output:**
```json
{
  "status": "success",
  "growth_mb": 50.0,
  "type_differences": [
    {
      "type": "MyApp.DataCache",
      "baseline_count": 1000,
      "comparison_count": 5000,
      "delta": 4000
    }
  ]
}
```

### Symbol and Source Tools

#### resolve_symbols

Resolve symbols for a trace.

**Input:**
```json
{
  "trace_path": "./trace.etl",
  "symbol_path": "SRV*C:\\Symbols*https://msdl.microsoft.com/download/symbols"
}
```

**Output:**
```json
{
  "status": "success",
  "resolved_modules": 25,
  "failed_modules": 2,
  "resolution_percentage": 92.6
}
```

#### get_source_location

Find source file for a method.

**Input:**
```json
{
  "method_name": "MyApp.MyClass.MyMethod",
  "module_path": "C:\\app\\MyApp.dll"
}
```

**Output:**
```json
{
  "status": "success",
  "source_file": "C:\\Source\\MyApp\\MyClass.cs",
  "line_number": 42
}
```

### Report Tools

#### generate_html_report

Generate HTML report.

**Input:**
```json
{
  "trace_path": "./trace.etl",
  "report_type": "cpu",
  "output_path": "./report.html"
}
```

**Output:**
```json
{
  "status": "success",
  "output_path": "./report.html",
  "report_size_kb": 125.5
}
```

## Integration with AI Assistants

### Claude Desktop Configuration

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "perfview": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\perfview\\src\\PerfView.MCPServer\\PerfView.MCPServer.csproj"
      ]
    }
  }
}
```

### Example AI Conversation

```
User: Can you analyze the performance of my application?

Claude: I'll help you collect and analyze a performance trace. Let me start by 
collecting a CPU trace for 30 seconds.

[Calls collect_cpu_trace tool]

The trace has been collected successfully. Now let me analyze the CPU hotspots.

[Calls analyze_cpu_hotspots tool]

I found the following performance bottlenecks:
1. MyApp.ProcessData - 35% CPU time
2. System.Text.Json.JsonSerializer.Deserialize - 20% CPU time
3. Database.ExecuteQuery - 15% CPU time

Would you like me to investigate any of these methods further?
```

## Architecture

```
┌─────────────────────┐
│   AI Assistant      │
│  (Claude, GPT, etc) │
└──────────┬──────────┘
           │ MCP Protocol (JSON-RPC over stdio)
           │
┌──────────▼──────────┐
│  PerfView.MCPServer │
├─────────────────────┤
│  • 12 Tools         │
│  • JSON I/O         │
│  • Logging          │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│   PerfView Core     │
├─────────────────────┤
│  • TraceEvent       │
│  • MemoryGraph      │
│  • Symbols          │
└─────────────────────┘
```

## Error Handling

All tools return structured error responses:

```json
{
  "status": "error",
  "error_code": "INSUFFICIENT_PRIVILEGES",
  "message": "Administrator privileges required for kernel-mode tracing",
  "suggestion": "Run the MCP server with elevated permissions"
}
```

Common error codes:
- `INSUFFICIENT_PRIVILEGES` - Need admin rights
- `FILE_NOT_FOUND` - Trace/snapshot file missing
- `INVALID_PARAMETERS` - Bad input parameters
- `TRACE_TOO_LARGE` - File exceeds size limits
- `SYMBOL_RESOLUTION_FAILED` - Can't resolve symbols

## Security Considerations

1. **Elevation**: Many operations require admin privileges
2. **File Access**: Validates paths to prevent directory traversal
3. **Resource Limits**: Implements timeouts and size limits
4. **Credentials**: Securely handles symbol server credentials

## Performance Tips

1. **Trace Duration**: Keep traces under 5 minutes for faster analysis
2. **Process Filtering**: Use process filters to reduce trace size
3. **Symbol Caching**: Configure symbol cache directory for faster resolution
4. **Streaming**: Use query limits when working with large traces

## Troubleshooting

### Server Won't Start

- Check .NET 8.0 SDK is installed: `dotnet --version`
- Verify dependencies: `dotnet restore`
- Check logs for error messages

### Tools Return Errors

- Verify file paths exist and are accessible
- Check if admin privileges are needed
- Ensure sufficient disk space for traces
- Verify symbol server connectivity

### MCP Client Can't Connect

- Verify stdio transport is working
- Check MCP client configuration
- Review server logs for connection errors

## Next Steps

1. **Verify MCP Protocol**: Confirm ModelContextProtocol API compatibility
2. **Integrate TraceEvent**: Connect tools to actual ETW collection/parsing
3. **Add Real Analysis**: Implement actual stack analysis and hotspot detection
4. **Symbol Resolution**: Integrate with PerfView's symbol services
5. **Testing**: Create comprehensive test suite with real traces

## Resources

- [Full Specification](./PerfViewMCPServer.md)
- [PerfView Documentation](https://github.com/microsoft/perfview/tree/main/documentation)
- [TraceEvent Library Guide](./TraceEvent/TraceEventLibrary.md)
- [Model Context Protocol Spec](https://spec.modelcontextprotocol.io/)

## Support

For issues or questions:
- GitHub Issues: https://github.com/microsoft/perfview/issues
- Discussions: https://github.com/microsoft/perfview/discussions
