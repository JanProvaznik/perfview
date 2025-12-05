# PerfView MCP Server

> **Status**: ✅ Functional and operational with ModelContextProtocol 0.4.0-preview.3

## Overview

The PerfView MCP Server enables AI assistants to automate performance analysis workflows through the Model Context Protocol. It provides programmatic access to trace collection, CPU analysis, and event querying.

## Architecture

```
AI Assistant (Claude) 
    ↓ MCP Protocol (JSON-RPC over stdio)
PerfView.MCPServer
    ↓ .NET APIs
TraceEvent Library
    ↓
ETW / EventPipe Tracing
```

## Implemented Tools (4)

### 1. collect_cpu_trace
Collects CPU sampling traces using ETW kernel providers.

**Use Cases:**
- Profile application performance
- Identify CPU bottlenecks
- Capture system-wide activity

**Requirements:** Administrator privileges on Windows

### 2. analyze_cpu_hotspots
Analyzes trace files to identify CPU-intensive code paths.

**Returns:** Top N methods by CPU consumption with percentages

### 3. query_events
Queries and filters ETW/EventPipe events from traces.

**Features:**
- Filter by provider and event name
- Show timestamps and payloads
- Configurable result limits

### 4. get_trace_stats
Provides high-level trace file statistics.

**Returns:** Duration, event counts, top providers and processes

## Key Scenarios

### Automated Performance Investigation
AI assistants automatically collect traces, analyze hotspots, and provide actionable recommendations.

### Cross-Platform Workflow
Use dotnet-trace on Linux/macOS for collection, analyze with PerfView MCP on Windows:
```bash
# Linux/macOS
dotnet-trace collect --process-id <PID> --output app.nettrace

# Windows - AI analyzes via MCP
"Analyze CPU hotspots in app.nettrace"
```

### Continuous Monitoring
Periodic trace collection with AI-assisted analysis for regression detection.

### Event Investigation
Natural language queries for specific events:
```
"Show me all exceptions in the trace"
"What GC events took longer than 100ms?"
```

## Technology

- **Platform**: .NET 8.0
- **MCP Version**: ModelContextProtocol 0.4.0-preview.3
- **Core Library**: Microsoft.Diagnostics.Tracing.TraceEvent
- **Transport**: stdio (JSON-RPC 2.0)

## Security

### Privilege Requirements
- **Trace Collection**: Administrator (Windows) for kernel-mode ETW
- **Analysis Tools**: User-level access sufficient

### Future Enhancement
Just-in-time privilege escalation via UAC prompts when elevated access is needed, avoiding requirement to run entire server as admin.

## Configuration

See [`src/PerfView.MCPServer/README.md`](../src/PerfView.MCPServer/README.md) for:
- Claude Desktop configuration
- Common scenarios with dotnet-trace
- Usage examples
- Troubleshooting

## Future Tools (Planned)

- Memory analysis tools (heap analysis, GC investigation)
- Trace comparison tools (diff operations)
- Symbol resolution tools
- Report generation tools

## References

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [dotnet-trace Documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
- [TraceEvent Library](https://github.com/microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventLibrary.md)
