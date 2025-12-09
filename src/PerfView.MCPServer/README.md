# PerfView MCP Server

AI-accessible performance analysis through the Model Context Protocol.

## Quick Start

```bash
cd src/PerfView.MCPServer
dotnet run
```

The server communicates via stdin/stdout using the MCP protocol.

## Configuration

### Claude Desktop

Add to your config file (`%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "perfview": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\perfview\\src\\PerfView.MCPServer\\PerfView.MCPServer.csproj"]
    }
  }
}
```

## Available Tools

### 1. collect_cpu_trace
Collects CPU sampling traces. Administrator privileges required on Windows.

**Parameters:**
- `durationSeconds` (int, default: 30): Collection duration
- `outputPath` (string, default: "./trace.etl"): Output file path
- `processFilter` (string, optional): Process name filter

### 2. analyze_cpu_hotspots
Analyzes CPU hotspots in a trace file.

**Parameters:**
- `tracePath` (string): Path to ETL trace file
- `topN` (int, default: 10): Number of top methods
- `processFilter` (string, optional): Process name filter

### 3. query_events
Queries and filters ETW events from a trace.

**Parameters:**
- `tracePath` (string): Path to trace file
- `providerName` (string, optional): Provider filter
- `eventName` (string, optional): Event filter
- `limit` (int, default: 100): Max events

### 4. get_trace_stats
Gets trace file statistics.

**Parameters:**
- `tracePath` (string): Path to trace file

## Common Scenarios

### Scenario 1: Analyze .NET Application Performance

Collect a trace with dotnet-trace, then analyze with PerfView MCP:

```bash
# 1. Collect trace with dotnet-trace
dotnet-trace collect --process-id <PID> --duration 00:00:30 --output app.nettrace

# 2. Convert to ETL (if needed)
# PerfView can read .nettrace files directly on newer versions

# 3. Ask AI to analyze
"Analyze the CPU hotspots in app.nettrace"
```

### Scenario 2: Cross-Platform Trace Analysis

Use dotnet-trace on Linux/macOS, analyze with PerfView MCP on Windows:

```bash
# On Linux/macOS - collect EventPipe trace
dotnet-trace collect --process-id <PID> --providers Microsoft-DotNETCore-SampleProfiler

# Transfer app.nettrace to Windows

# On Windows - analyze with AI
"What are the top CPU consuming methods in app.nettrace?"
```

### Scenario 3: Continuous Performance Monitoring

Automated trace collection and analysis:

```bash
# 1. Collect trace periodically
dotnet-trace collect --process-id <PID> --duration 00:01:00 --output hourly-$(date +%H).nettrace

# 2. AI-assisted analysis
"Compare hourly-10.nettrace and hourly-11.nettrace for performance changes"
```

### Scenario 4: Investigate Specific Events

Target specific providers for detailed investigation:

```bash
# Collect with specific providers
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1F000080018:5

# Analyze events
"Show me all GC events from the trace where duration > 100ms"
```

### Scenario 5: Memory Allocation Analysis

Track allocations and analyze patterns:

```bash
# Collect with allocation tracking
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-DotNETCore-SampleProfiler,Microsoft-Windows-DotNETRuntime:0x1:4

# Analyze
"What types are allocating the most memory?"
```

## dotnet-trace Integration

[dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) is a cross-platform tool for collecting .NET traces. It works well with PerfView MCP:

### Installation
```bash
dotnet tool install --global dotnet-trace
```

### Common Commands

**List running .NET processes:**
```bash
dotnet-trace ps
```

**Collect trace with default providers:**
```bash
dotnet-trace collect --process-id <PID>
```

**Collect with CPU sampling:**
```bash
dotnet-trace collect --process-id <PID> --profile cpu-sampling
```

**Collect with GC events:**
```bash
dotnet-trace collect --process-id <PID> --profile gc-verbose
```

**Custom providers:**
```bash
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1CCBD:4
```

### Converting Traces

dotnet-trace creates `.nettrace` files (EventPipe format). PerfView can analyze these directly, or convert to ETL:

```bash
# In PerfView GUI: File > Open > Select .nettrace file
# Or use PerfView command line:
PerfView.exe /convert app.nettrace
```

## Usage Examples

### Example 1: AI-Assisted Investigation

```
User: I need to find why my .NET app is slow

AI: Let me collect a trace for 30 seconds
    [Calls collect_cpu_trace]
    ✓ Trace collected: trace.etl (42 MB)
    
    Analyzing CPU hotspots...
    [Calls analyze_cpu_hotspots]
    
    Top issues found:
    1. 35% CPU - Database queries in loop
    2. 18% CPU - JSON serialization
    3. 12% CPU - String allocations
    
    Recommendation: Batch database calls and use Span<T>
```

### Example 2: Event Investigation

```
User: Show me all exceptions in the trace

AI: [Calls query_events with provider filter]
    Found 15 exception events:
    
    [10:23:45] System.NullReferenceException
    [10:24:12] System.InvalidOperationException
    ...
```

## Privilege Escalation

The server implements **automatic just-in-time privilege elevation**:

### How It Works

When you request trace collection without administrator privileges:

1. **Automatic Detection**: Server detects current privilege level
2. **UAC Elevation**: Spawns an elevated helper process (triggers UAC prompt on Windows)
3. **Seamless Operation**: Helper performs trace collection with elevated privileges
4. **Results Return**: Trace file is created and results returned to MCP server

**You don't need to run the entire MCP server as administrator!**

### Platform Support

- **Windows**: Uses UAC (User Account Control) to spawn elevated helper
  - Triggers standard Windows elevation prompt
  - User approves once per trace collection
  
- **Linux/macOS**: Uses sudo to spawn elevated helper
  - May require sudo password
  - Respects system sudo timeout

### User Experience

```
User: Collect a 30-second CPU trace

Claude: [Calls collect_cpu_trace tool]
        → Server detects non-elevated
        → Spawns PerfView.MCPServer.TraceHelper with elevation
        → [UAC prompt appears - user clicks Yes]
        → Trace collection proceeds
        → ✓ CPU trace collected successfully (with elevation)
```

### Technical Details

The server uses a separate helper executable (`PerfView.MCPServer.TraceHelper`) for privileged operations:
- Helper is a minimal console app that only performs trace collection
- Spawned with `runas` verb on Windows (UAC) or `sudo` on Unix
- Communicates results back through exit codes and output files
- MCP server remains non-elevated, maintaining stdio connection

### Advantages

✅ **No admin required**: MCP server runs as regular user  
✅ **On-demand elevation**: Only when collecting traces  
✅ **UAC integration**: Native Windows security prompts  
✅ **Stable connection**: MCP client connection unaffected  
✅ **Better security**: Minimal elevated code surface

## Troubleshooting

### Build Issues
```bash
dotnet restore
dotnet build
```

### Permission Errors
Run terminal/PowerShell as Administrator or use `sudo` on Linux.

### Package Issues
Verify ModelContextProtocol 0.4.0-preview.3 is installed:
```bash
dotnet list package | grep ModelContextProtocol
```

## Testing

### Manual Test
```bash
dotnet run
# Server waits for MCP protocol messages on stdin
```

### With MCP Client
Configure Claude Desktop (see Configuration section), then ask:
- "What tools do you have for performance analysis?"
- "Collect a 10-second CPU trace"

## Architecture

```
AI Assistant (Claude)
      ↓ MCP Protocol (stdio)
PerfView.MCPServer
      ↓ TraceEvent Library
ETW / EventPipe
```

## Requirements

- .NET 8.0 Runtime
- Windows 10+ (for ETW tracing)
- Administrator privileges (for kernel tracing)
- Linux/macOS supported for EventPipe trace analysis only

## Documentation

Full specification: [`documentation/PerfViewMCPServer.md`](../../documentation/PerfViewMCPServer.md)

## Support

- GitHub Issues: https://github.com/microsoft/perfview/issues
- dotnet-trace docs: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace
- MCP Specification: https://spec.modelcontextprotocol.io/
