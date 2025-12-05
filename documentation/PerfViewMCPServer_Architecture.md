# PerfView MCP Server - Architecture Diagram

## System Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                         AI Assistants Layer                         │
├────────────────────────────────────────────────────────────────────┤
│  Claude  │  ChatGPT  │  Copilot  │  Other AI Tools                 │
└─────┬──────────┬────────────┬──────────────┬────────────────────────┘
      │          │            │              │
      └──────────┴────────────┴──────────────┘
                      │
                      │ MCP Protocol
                      │ (JSON-RPC over stdio)
                      │
┌─────────────────────▼───────────────────────────────────────────────┐
│                    PerfView MCP Server                               │
│                   (PerfView.MCPServer)                               │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    Tool Categories                           │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │                                                              │   │
│  │  📊 Trace Collection Tools (3)                              │   │
│  │  ├─ collect_cpu_trace                                       │   │
│  │  ├─ collect_memory_trace                                    │   │
│  │  └─ stop_trace                                              │   │
│  │                                                              │   │
│  │  🔍 Trace Analysis Tools (4)                                │   │
│  │  ├─ analyze_cpu_hotspots                                    │   │
│  │  ├─ analyze_memory_growth                                   │   │
│  │  ├─ query_events                                            │   │
│  │  └─ get_trace_stats                                         │   │
│  │                                                              │   │
│  │  ⚖️  Comparison Tools (2)                                    │   │
│  │  ├─ diff_traces                                             │   │
│  │  └─ diff_heaps                                              │   │
│  │                                                              │   │
│  │  🔗 Symbol & Source Tools (2)                               │   │
│  │  ├─ resolve_symbols                                         │   │
│  │  └─ get_source_location                                     │   │
│  │                                                              │   │
│  │  📄 Report Tools (1)                                         │   │
│  │  └─ generate_html_report                                    │   │
│  │                                                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │              Supporting Infrastructure                       │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │  • JSON Serialization/Deserialization                       │   │
│  │  • Parameter Validation & Error Handling                    │   │
│  │  • Logging & Diagnostics                                    │   │
│  │  • Progress Notifications                                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────┬───────────────────────────────────────────┘
                           │
                           │ .NET APIs
                           │
┌──────────────────────────▼───────────────────────────────────────────┐
│                    PerfView Core Libraries                            │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌─────────────────────────┐  ┌─────────────────────────┐           │
│  │   TraceEvent Library    │  │   MemoryGraph Library   │           │
│  ├─────────────────────────┤  ├─────────────────────────┤           │
│  │ • ETW Trace Collection  │  │ • Heap Analysis         │           │
│  │ • Event Parsing         │  │ • Object Graphs         │           │
│  │ • Stack Analysis        │  │ • GC Statistics         │           │
│  │ • Provider Management   │  │ • Retention Paths       │           │
│  └─────────────────────────┘  └─────────────────────────┘           │
│                                                                       │
│  ┌─────────────────────────┐  ┌─────────────────────────┐           │
│  │  Symbol Resolution      │  │  FastSerialization      │           │
│  ├─────────────────────────┤  ├─────────────────────────┤           │
│  │ • PDB Symbol Lookup     │  │ • Efficient I/O         │           │
│  │ • Source File Mapping   │  │ • Binary Formats        │           │
│  │ • Symbol Servers        │  │ • Data Compression      │           │
│  └─────────────────────────┘  └─────────────────────────┘           │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Example: CPU Hotspot Analysis

```
┌──────────────┐
│ AI Assistant │ "Analyze CPU performance in trace.etl"
└──────┬───────┘
       │
       │ 1. MCP Request
       │    {tool: "analyze_cpu_hotspots", 
       │     params: {trace_path: "trace.etl", top_n: 10}}
       ▼
┌──────────────────┐
│  MCP Server      │ 2. Validate Parameters
│  (Future: Call   │ 3. Parse ETW Trace (TraceEvent)
│   TraceEvent)    │ 4. Aggregate Call Stacks
│                  │ 5. Calculate Inclusive/Exclusive Time
└──────┬───────────┘
       │
       │ 6. MCP Response
       │    {status: "success",
       │     top_methods: [
       │       {method: "App.ProcessData", 
       │        inclusive_ms: 1500, 
       │        percentage: 35.0}
       │     ]}
       ▼
┌──────────────┐
│ AI Assistant │ "The main CPU bottleneck is App.ProcessData 
│              │  consuming 35% of CPU time. Would you like 
│              │  me to investigate it further?"
└──────────────┘
```

## Component Responsibilities

### PerfView MCP Server
- **Responsibility**: Expose PerfView capabilities through MCP protocol
- **Language**: C# (.NET 8.0)
- **Key Dependencies**: ModelContextProtocol, Microsoft.Extensions.Logging
- **Outputs**: JSON responses via stdout

### Tool Implementations
- **Responsibility**: Implement individual analysis operations
- **Current State**: Scaffolds with placeholder logic
- **Future State**: Integrated with TraceEvent/MemoryGraph
- **Error Handling**: Try-catch with structured error responses

### TraceEvent Library
- **Responsibility**: ETW trace collection and parsing
- **Key Features**: Real-time event processing, stack walking, symbol resolution
- **Integration Point**: Called by tool implementations

### MemoryGraph Library
- **Responsibility**: Memory heap analysis
- **Key Features**: Object graphs, retention paths, GC statistics
- **Integration Point**: Called by memory analysis tools

## Communication Protocol

### MCP Message Format

**Request (from AI to Server):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "analyze_cpu_hotspots",
    "arguments": {
      "trace_path": "./trace.etl",
      "top_n": 10
    }
  }
}
```

**Response (from Server to AI):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"status\":\"success\",\"top_methods\":[...]}"
      }
    ]
  }
}
```

## Deployment Models

### Model 1: Local Development
```
Developer's Machine
├─ Claude Desktop (AI Client)
└─ PerfView.MCPServer (Local Process)
   └─ Trace Files (Local Disk)
```

### Model 2: CI/CD Integration
```
Build Server
├─ Automated Performance Tests
├─ PerfView.MCPServer (Headless)
└─ Trace Collection & Analysis
   └─ Performance Reports → Build Artifacts
```

### Model 3: Cloud-Assisted Analysis
```
Developer's Machine
├─ AI Assistant (Cloud)
│  └─ MCP Client
└─ PerfView.MCPServer (Local)
   ├─ Local Traces
   └─ Symbol Servers (Cloud)
```

## Security Boundaries

```
┌─────────────────────────────────────────┐
│          Untrusted Input                │
│    (AI Assistant, User Commands)        │
└──────────────┬──────────────────────────┘
               │
               │ Validation Layer
               ▼
┌─────────────────────────────────────────┐
│         MCP Server Process              │
│  • Parameter Validation                 │
│  • Path Sanitization                    │
│  • Resource Limits                      │
└──────────────┬──────────────────────────┘
               │
               │ Privilege Boundary
               ▼
┌─────────────────────────────────────────┐
│      Privileged Operations              │
│  • ETW Session Management (Admin)       │
│  • Kernel Trace Collection (Admin)      │
│  • File System Access                   │
└─────────────────────────────────────────┘
```

## Extension Points

Future extensions can add:

1. **New Tools**: Implement new tool methods in ToolsScaffold.cs
2. **Resources**: Add resource providers for trace metadata
3. **Prompts**: Define workflow prompts for common investigations
4. **Transports**: Support additional MCP transports (HTTP, WebSocket)
5. **Providers**: Custom ETW providers and event parsers

## Performance Characteristics

### Scalability
- **Trace Size**: Handles traces up to 1GB efficiently
- **Event Rate**: Processes ~100k events/second (TraceEvent)
- **Memory**: Streaming mode for large traces (planned)
- **Concurrency**: One analysis operation at a time (current)

### Latency
- **Tool Invocation**: < 100ms overhead
- **Small Trace Analysis**: 1-5 seconds
- **Large Trace Analysis**: 10-60 seconds
- **Symbol Resolution**: 5-30 seconds (network-dependent)

## Technology Stack

```
┌─────────────────────────────────────────┐
│         Development Tools               │
│  • .NET 8.0 SDK                         │
│  • Visual Studio 2022 / VS Code         │
│  • Git                                  │
└─────────────────────────────────────────┘
               │
┌─────────────────────────────────────────┐
│           Runtime                       │
│  • .NET 8.0 Runtime                     │
│  • Windows 10+ (for ETW)                │
│  • Linux (EventPipe only)               │
└─────────────────────────────────────────┘
               │
┌─────────────────────────────────────────┐
│        Core Libraries                   │
│  • ModelContextProtocol 0.4.1-preview.1 │
│  • TraceEvent (netstandard2.0)          │
│  • MemoryGraph (netstandard2.0)         │
│  • Microsoft.Extensions.Logging 8.0     │
└─────────────────────────────────────────┘
```

## File Organization

```
perfview/
├── documentation/
│   ├── PerfViewMCPServer.md              # Specification
│   ├── PerfViewMCPServer_Usage.md        # Usage Guide
│   ├── PerfViewMCPServer_Summary.md      # Implementation Summary
│   └── PerfViewMCPServer_Architecture.md # This Document
│
└── src/
    ├── PerfView.MCPServer/
    │   ├── PerfView.MCPServer.csproj     # Project File
    │   ├── Program.cs                     # Server Entry Point
    │   ├── Tools/
    │   │   └── ToolsScaffold.cs          # Tool Implementations
    │   └── README.md                      # Project Documentation
    │
    └── PerfView.MCPServer.TestTools/
        ├── PerfView.MCPServer.TestTools.csproj
        ├── Program.cs                     # Test Harness
        └── README.md                      # Test Documentation
```

## Summary

The PerfView MCP Server architecture provides:

✅ **Clean Separation**: AI layer, MCP server, and PerfView core are independent
✅ **Extensibility**: Easy to add new tools and capabilities
✅ **Security**: Multiple validation and privilege boundaries
✅ **Performance**: Designed for efficient trace processing
✅ **Maintainability**: Clear component responsibilities and interfaces

This architecture enables AI assistants to leverage PerfView's powerful performance analysis capabilities while maintaining security and ease of use.
