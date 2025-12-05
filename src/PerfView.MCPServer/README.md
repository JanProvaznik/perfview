# PerfView MCP Server

An AI-accessible performance analysis server that exposes PerfView's capabilities through the Model Context Protocol (MCP).

## Overview

The PerfView MCP Server allows AI assistants (like Claude) to automate performance investigations, trace analysis, and memory profiling. It bridges the gap between AI-powered development tools and PerfView's powerful diagnostic capabilities.

## Features

### Trace Collection
- **CPU Trace Collection**: Start CPU sampling traces with custom duration and filters
- **Memory Snapshots**: Capture GC heap snapshots for memory analysis
- **Trace Session Management**: Start, stop, and manage trace sessions

### Trace Analysis
- **CPU Hotspot Analysis**: Identify CPU-intensive methods and call stacks
- **Memory Growth Analysis**: Detect memory leaks and growth patterns
- **Event Querying**: Query and filter ETW events with flexible criteria
- **Trace Statistics**: Get high-level overview of trace contents

### Comparison
- **Trace Diffing**: Compare two traces to find performance regressions
- **Heap Diffing**: Compare memory snapshots to identify growth areas

### Symbol Resolution
- **Symbol Lookup**: Resolve symbols for addresses and modules
- **Source Navigation**: Find source file locations for methods

### Reporting
- **HTML Report Generation**: Create detailed performance reports

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Windows OS (for ETW trace collection)
- Administrator privileges (for kernel-mode tracing)

### Building

```bash
cd src/PerfView.MCPServer
dotnet build
```

### Running

```bash
dotnet run --project src/PerfView.MCPServer/PerfView.MCPServer.csproj
```

The server uses standard I/O (stdin/stdout) for MCP communication.

## Configuration with Claude Desktop

Add the following to your Claude Desktop configuration file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

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

## Usage Examples

### Example 1: Collect and Analyze CPU Trace

```
User: Can you help me find CPU hotspots in my application?

Claude: I'll collect a CPU trace and analyze it for you.
[Uses collect_cpu_trace tool]
[Uses analyze_cpu_hotspots tool]

The top CPU consumers are:
1. MyApp.DataProcessor.ProcessItems - 45% CPU
2. System.Text.Json.JsonSerializer.Deserialize - 20% CPU
3. MyApp.Network.SendData - 15% CPU

Would you like me to investigate any of these methods further?
```

### Example 2: Diagnose Memory Leak

```
User: My application's memory keeps growing. Can you help?

Claude: Let me collect two memory snapshots and compare them.
[Uses collect_memory_trace for baseline]
[User runs workload]
[Uses collect_memory_trace for comparison]
[Uses diff_heaps tool]

I found the following memory growth:
- System.String: +5000 objects (+5 MB)
- MyApp.Cache: +4000 objects (+25 MB)

The MyApp.Cache is likely causing the leak. Would you like me to investigate retention paths?
```

## Tool Reference

### collect_cpu_trace

Collect a CPU sampling trace.

**Parameters:**
- `duration_seconds` (number): Collection duration
- `output_path` (string): Output file path
- `process_filter` (string, optional): Process name filter
- `providers` (array, optional): Additional ETW providers

**Returns:** Trace file path and statistics

### analyze_cpu_hotspots

Analyze CPU hotspots in a trace.

**Parameters:**
- `trace_path` (string): Path to trace file
- `process_filter` (string, optional): Process name filter
- `top_n` (number, default: 10): Number of top methods

**Returns:** List of CPU-intensive methods

### query_events

Query ETW events from a trace.

**Parameters:**
- `trace_path` (string): Path to trace file
- `provider_name` (string, optional): ETW provider filter
- `event_name` (string, optional): Event name filter
- `time_range` (object, optional): Time range filter
- `limit` (number, default: 100): Max events to return

**Returns:** Filtered event list

### diff_traces

Compare two traces for performance differences.

**Parameters:**
- `baseline_trace` (string): Baseline trace path
- `comparison_trace` (string): Comparison trace path
- `metric` (string): Comparison metric (cpu, memory, events)

**Returns:** Performance deltas and regressions

For complete tool documentation, see [PerfViewMCPServer.md](../../documentation/PerfViewMCPServer.md).

## Architecture

```
AI Assistant (Claude)
        ↓
  MCP Protocol (JSON-RPC over stdio)
        ↓
PerfView.MCPServer (This project)
        ↓
PerfView Core Libraries
    ├── TraceEvent (ETW parsing)
    ├── MemoryGraph (Heap analysis)
    └── FastSerialization (Data handling)
```

## Development

### Project Structure

```
PerfView.MCPServer/
├── Program.cs              # Entry point and server setup
├── Tools/                  # MCP tool implementations
│   ├── TraceCollectionTools.cs
│   ├── TraceAnalysisTools.cs
│   ├── ComparisonTools.cs
│   ├── SymbolTools.cs
│   └── ReportTools.cs
└── README.md              # This file
```

### Adding New Tools

1. Create a new tool class in the `Tools/` directory
2. Implement tool methods with signature: `Task<ToolResponse> ToolName(ToolRequest request)`
3. Register the tool in `Program.cs` using `server.AddTool()`

### Testing

```bash
dotnet test
```

## Roadmap

- [ ] Phase 1: Core infrastructure ✅ (Complete)
- [ ] Phase 2: Trace collection implementation
- [ ] Phase 3: Analysis tools implementation
- [ ] Phase 4: Advanced features (diff, symbols, reports)
- [ ] Phase 5: Testing and documentation

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the same terms as PerfView. See [LICENSE.TXT](../../LICENSE.TXT).

## Support

For issues and questions:
- File an issue: https://github.com/microsoft/perfview/issues
- Discussions: https://github.com/microsoft/perfview/discussions

## Related Resources

- [PerfView Documentation](../../documentation)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [TraceEvent Library Guide](../../documentation/TraceEvent/TraceEventLibrary.md)
