# PerfView MCP Server Specification

## Overview

The PerfView MCP (Model Context Protocol) Server provides AI assistants and developer tools with programmatic access to PerfView's powerful performance analysis and diagnostic capabilities. This enables intelligent automation of performance investigations, trace analysis, and memory profiling workflows.

## What is MCP?

Model Context Protocol (MCP) is an open protocol that enables seamless integration between AI applications and external data sources. It allows AI assistants to access tools, prompts, and resources in a standardized way, making it easier to connect AI systems with specialized tools like PerfView.

## Key Scenarios

### 1. **Automated Performance Investigation**
- **Problem**: Performance investigations are manual and time-consuming
- **MCP Solution**: AI assistants can automatically collect traces, analyze CPU hotspots, and generate reports
- **Benefit**: Faster root cause analysis with guided investigation flows

### 2. **Memory Leak Detection and Analysis**
- **Problem**: Memory leaks require expert knowledge to diagnose
- **MCP Solution**: Expose heap analysis, GC stats, and memory growth patterns through MCP tools
- **Benefit**: AI can guide developers through memory profiling workflows

### 3. **Trace Collection and Management**
- **Problem**: Collecting the right trace with correct settings is complex
- **MCP Solution**: Provide tools to start/stop traces with optimal configurations
- **Benefit**: Simplified trace collection with best practices built-in

### 4. **Performance Regression Analysis**
- **Problem**: Comparing performance between builds is tedious
- **MCP Solution**: Automate diff operations between traces and highlight changes
- **Benefit**: Quick identification of performance regressions

### 5. **Symbol Resolution and Source Navigation**
- **Problem**: Setting up symbol servers and resolving symbols is error-prone
- **MCP Solution**: Expose symbol lookup and source file retrieval capabilities
- **Benefit**: Automated symbol setup and navigation

### 6. **ETW Event Analysis**
- **Problem**: Understanding ETW events requires domain expertise
- **MCP Solution**: Query and filter ETW events with natural language
- **Benefit**: More accessible event analysis for all developers

### 7. **Batch Analysis and Reporting**
- **Problem**: Analyzing multiple traces manually is inefficient
- **MCP Solution**: Automate analysis across multiple trace files
- **Benefit**: Scalable performance monitoring and reporting

## Design Considerations

### Architecture

```
┌─────────────────┐
│   AI Assistant  │
│  (Claude, etc)  │
└────────┬────────┘
         │ MCP Protocol
         │ (JSON-RPC)
         │
┌────────▼────────┐
│  PerfView MCP   │
│     Server      │
├─────────────────┤
│  • Tools        │
│  • Resources    │
│  • Prompts      │
└────────┬────────┘
         │
┌────────▼────────┐
│  PerfView Core  │
│   Libraries     │
├─────────────────┤
│  • TraceEvent   │
│  • MemoryGraph  │
│  • Symbols      │
└─────────────────┘
```

### Technology Stack

- **Platform**: .NET 6.0+ (for modern cross-platform support)
- **MCP Library**: ModelContextProtocol 0.4.1-preview.1
- **Core Dependencies**: 
  - Microsoft.Diagnostics.Tracing.TraceEvent (ETW/EventPipe trace parsing)
  - Microsoft.Diagnostics.Runtime (Memory dump analysis)
  - FastSerialization (Efficient data handling)

### Communication Model

- **Transport**: Standard I/O (stdin/stdout) for MCP protocol
- **Message Format**: JSON-RPC 2.0
- **Async Operations**: Long-running operations (trace collection) use MCP progress notifications

### Security Considerations

1. **Privilege Escalation**: Many PerfView operations require admin rights
   - Server should clearly communicate when elevation is needed
   - Provide safe fallback modes for non-admin scenarios

2. **File System Access**: Traces can be large and contain sensitive data
   - Implement path validation and sandboxing
   - Respect user-defined data directories

3. **Resource Limits**: Trace analysis can be memory-intensive
   - Implement timeouts for long-running operations
   - Provide streaming APIs for large datasets

4. **Credential Management**: Symbol server access requires authentication
   - Securely handle credentials
   - Support credential providers (Azure, PAT tokens)

## MCP Server Capabilities

### Tools

MCP tools are functions that the AI can call to perform actions.

#### 1. Trace Collection Tools

**`collect_cpu_trace`**
- **Description**: Start CPU sampling trace collection
- **Parameters**:
  - `duration_seconds` (number): How long to collect
  - `output_path` (string): Where to save the .etl file
  - `process_filter` (string, optional): Process name to focus on
  - `providers` (array, optional): Additional ETW providers
- **Returns**: Trace file path and basic statistics

**`collect_memory_trace`**
- **Description**: Collect GC heap snapshot
- **Parameters**:
  - `process_name` (string): Target process name or PID
  - `output_path` (string): Where to save the snapshot
  - `freeze_process` (boolean): Whether to freeze during collection
- **Returns**: Snapshot file path and heap size

**`stop_trace`**
- **Description**: Stop an active trace session
- **Parameters**:
  - `session_name` (string): Name of the trace session
- **Returns**: Final trace file location

#### 2. Trace Analysis Tools

**`analyze_cpu_hotspots`**
- **Description**: Identify CPU-intensive methods in a trace
- **Parameters**:
  - `trace_path` (string): Path to .etl or .etl.zip file
  - `process_filter` (string, optional): Focus on specific process
  - `top_n` (number, default: 10): How many top methods to return
- **Returns**: List of methods with inclusive/exclusive time

**`analyze_memory_growth`**
- **Description**: Detect memory leaks and growth patterns
- **Parameters**:
  - `snapshot_path` (string): Path to heap snapshot
  - `baseline_path` (string, optional): Previous snapshot for comparison
- **Returns**: Memory growth analysis and leak suspects

**`query_events`**
- **Description**: Query and filter ETW events from a trace
- **Parameters**:
  - `trace_path` (string): Path to trace file
  - `provider_name` (string, optional): Filter by provider
  - `event_name` (string, optional): Filter by event name
  - `time_range` (object, optional): Start/end timestamps
  - `limit` (number, default: 100): Max events to return
- **Returns**: List of matching events with timestamps and payloads

**`get_trace_stats`**
- **Description**: Get high-level statistics about a trace file
- **Parameters**:
  - `trace_path` (string): Path to trace file
- **Returns**: Duration, process list, event counts, file size

#### 3. Comparison Tools

**`diff_traces`**
- **Description**: Compare two traces to find performance differences
- **Parameters**:
  - `baseline_trace` (string): Path to baseline trace
  - `comparison_trace` (string): Path to comparison trace
  - `metric` (string): What to compare (cpu, memory, events)
- **Returns**: Performance deltas and regressions

**`diff_heaps`**
- **Description**: Compare two heap snapshots
- **Parameters**:
  - `baseline_snapshot` (string): Path to first snapshot
  - `comparison_snapshot` (string): Path to second snapshot
- **Returns**: Object count differences and growth areas

#### 4. Symbol and Source Tools

**`resolve_symbols`**
- **Description**: Resolve symbols for a trace or address
- **Parameters**:
  - `trace_path` (string): Path to trace file
  - `symbol_path` (string, optional): Custom symbol server path
- **Returns**: Symbol resolution status

**`get_source_location`**
- **Description**: Get source file location for a method
- **Parameters**:
  - `method_name` (string): Fully qualified method name
  - `module_path` (string): Path to the module/assembly
- **Returns**: Source file path and line number

#### 5. Report Generation Tools

**`generate_html_report`**
- **Description**: Create an HTML report from trace analysis
- **Parameters**:
  - `trace_path` (string): Path to trace file
  - `report_type` (string): Type of report (cpu, memory, summary)
  - `output_path` (string): Where to save HTML
- **Returns**: Path to generated report

### Resources

MCP resources are data that the AI can read.

**`trace://[path]`**
- **Description**: Access trace file metadata and contents
- **MIME Type**: application/vnd.perfview.trace+json
- **Contents**: Trace metadata, process list, event summary

**`snapshot://[path]`**
- **Description**: Access heap snapshot information
- **MIME Type**: application/vnd.perfview.snapshot+json
- **Contents**: Heap size, GC stats, type statistics

**`symbols://[trace-path]`**
- **Description**: Access resolved symbols for a trace
- **MIME Type**: application/json
- **Contents**: Symbol cache status and resolved addresses

### Prompts

MCP prompts are pre-configured investigation workflows.

**`investigate-performance-regression`**
- **Description**: Guide through comparing two builds
- **Arguments**: baseline_trace, current_trace
- **Workflow**: Diff analysis → Hotspot identification → Source navigation

**`diagnose-memory-leak`**
- **Description**: Step-by-step memory leak investigation
- **Arguments**: snapshot_path
- **Workflow**: Heap analysis → Growth suspects → Retention paths

**`optimize-startup-time`**
- **Description**: Analyze application startup performance
- **Arguments**: trace_path
- **Workflow**: Timeline analysis → Module load analysis → Recommendations

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create new .NET 6.0+ console application project
2. Add ModelContextProtocol NuGet package (0.4.1-preview.1)
3. Set up MCP server with stdio transport
4. Implement basic tool registration and dispatch

### Phase 2: Trace Collection Tools
1. Implement `collect_cpu_trace` using TraceEvent library
2. Implement `collect_memory_trace` using heap dumping APIs
3. Add progress notifications for long operations
4. Handle elevation and permissions

### Phase 3: Analysis Tools
1. Implement `analyze_cpu_hotspots` with call stack aggregation
2. Implement `analyze_memory_growth` with heap walking
3. Implement `query_events` with efficient filtering
4. Add `get_trace_stats` for quick overviews

### Phase 4: Advanced Features
1. Implement diff tools (`diff_traces`, `diff_heaps`)
2. Add symbol resolution tools
3. Implement report generation
4. Add resource providers

### Phase 5: Testing and Documentation
1. Create unit tests for each tool
2. Add integration tests with sample traces
3. Write user documentation
4. Create example workflows

## Configuration

The MCP server will use a configuration file for settings:

```json
{
  "perfview_mcp_server": {
    "trace_directory": "%TEMP%\\PerfViewTraces",
    "symbol_path": "SRV*C:\\Symbols*https://msdl.microsoft.com/download/symbols",
    "max_trace_duration_seconds": 300,
    "max_trace_size_mb": 1024,
    "enable_kernel_mode": false,
    "log_level": "info"
  }
}
```

## Error Handling

### Common Error Scenarios
1. **Insufficient Permissions**: Clear message about need for elevation
2. **File Not Found**: Validate paths and provide suggestions
3. **Trace Too Large**: Implement streaming or sampling
4. **Symbol Resolution Failed**: Fallback to unresolved stacks with guidance

### Error Response Format
```json
{
  "error": {
    "code": "INSUFFICIENT_PRIVILEGES",
    "message": "This operation requires administrator privileges",
    "details": {
      "required_privilege": "SeSystemProfilePrivilege",
      "suggestion": "Run the MCP server with elevated permissions"
    }
  }
}
```

## Performance Considerations

1. **Lazy Loading**: Don't load entire traces into memory
2. **Streaming**: Stream events for large queries
3. **Caching**: Cache parsed trace metadata
4. **Timeouts**: Set reasonable timeouts for all operations
5. **Cancellation**: Support cancellation for long operations

## Future Enhancements

1. **Real-time Monitoring**: Stream live ETW events
2. **Machine Learning**: Anomaly detection in performance data
3. **Cloud Integration**: Upload traces to cloud storage
4. **Distributed Tracing**: Correlate traces across services
5. **Custom Analyzers**: Plugin system for domain-specific analysis
6. **Web UI**: Optional web dashboard for MCP server status

## Usage Examples

### Example 1: Automated Performance Investigation

```javascript
// AI assistant workflow
const trace = await mcp.callTool("collect_cpu_trace", {
  duration_seconds: 30,
  output_path: "./app_profile.etl",
  process_filter: "myapp.exe"
});

const hotspots = await mcp.callTool("analyze_cpu_hotspots", {
  trace_path: trace.path,
  top_n: 5
});

// AI presents findings to user
```

### Example 2: Memory Leak Diagnosis

```javascript
// Collect baseline snapshot
const baseline = await mcp.callTool("collect_memory_trace", {
  process_name: "myapp.exe",
  output_path: "./baseline.gcdump"
});

// ... run application workload ...

// Collect comparison snapshot
const current = await mcp.callTool("collect_memory_trace", {
  process_name: "myapp.exe", 
  output_path: "./current.gcdump"
});

const analysis = await mcp.callTool("diff_heaps", {
  baseline_snapshot: baseline.path,
  comparison_snapshot: current.path
});

// AI identifies leak suspects
```

## Integration with AI Assistants

### Recommended Prompts for AI

```
You are a performance analysis expert using PerfView. You have access to tools for:
- Collecting CPU and memory traces
- Analyzing performance hotspots
- Comparing traces to find regressions
- Investigating memory leaks

When a user asks about performance, guide them through:
1. Collecting appropriate traces
2. Analyzing the data
3. Interpreting results
4. Providing actionable recommendations

Always explain what you're doing and why.
```

### Capabilities Declaration

The AI assistant should be configured with:
- Knowledge of ETW, EventPipe, and performance profiling
- Understanding of .NET runtime internals
- Ability to interpret stack traces and call trees
- Familiarity with common performance patterns

## Testing Strategy

### Unit Tests
- Test each tool in isolation
- Mock TraceEvent library dependencies
- Validate input parameter handling

### Integration Tests
- Use sample trace files for consistent results
- Test end-to-end workflows
- Verify MCP protocol compliance

### Performance Tests
- Measure tool latency
- Test with various trace sizes
- Verify memory usage stays bounded

## Deployment

### Standalone Executable
- Single-file deployment with dependencies included
- Works with standard MCP client configurations
- No PerfView GUI required

### Configuration for Claude Desktop
```json
{
  "mcpServers": {
    "perfview": {
      "command": "path/to/PerfView.MCPServer.exe",
      "args": []
    }
  }
}
```

## Success Metrics

- **Adoption**: Number of active MCP server instances
- **Usage**: Tool invocation frequency and patterns
- **Performance**: Average response time per tool
- **Reliability**: Error rate and success rate of operations
- **Satisfaction**: User feedback and GitHub stars

## References

- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [PerfView Documentation](https://github.com/microsoft/perfview/blob/main/documentation)
- [TraceEvent Library](https://github.com/microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventLibrary.md)
- [ETW Documentation](https://docs.microsoft.com/en-us/windows/win32/etw/about-event-tracing)

## Conclusion

The PerfView MCP Server bridges the gap between AI assistants and powerful performance analysis tools. By exposing PerfView's capabilities through a standard protocol, we enable:

- **Democratized Performance Analysis**: Make advanced profiling accessible to all developers
- **Automated Workflows**: Reduce manual effort in performance investigations
- **Intelligent Guidance**: AI-assisted problem diagnosis and recommendations
- **Better Insights**: Faster identification of performance issues

This MCP server transforms PerfView from a manual GUI tool into an AI-accessible service, multiplying its impact and reach across the developer community.
