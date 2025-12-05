# PerfView MCP Server Test Tools

This is a simple test harness that demonstrates how the PerfView MCP Server tools can be invoked programmatically.

## Purpose

- Validate that all tool methods work correctly
- Show the expected JSON input/output format for each tool
- Demonstrate the tool API without requiring MCP protocol setup

## Running the Tests

```bash
cd src/PerfView.MCPServer.TestTools
dotnet run
```

## Expected Output

The test harness will invoke the following tools with sample parameters:

1. **collect_cpu_trace** - Simulate CPU trace collection
2. **analyze_cpu_hotspots** - Analyze CPU hotspots in a trace
3. **collect_memory_trace** - Simulate memory snapshot collection
4. **analyze_memory_growth** - Analyze memory growth patterns
5. **diff_traces** - Compare two traces for performance differences

Each tool returns a JSON response showing the results (currently placeholder data).

## Next Steps

Once the actual TraceEvent library integration is complete, these tests will:
- Use real trace files
- Generate actual performance data
- Validate against known test cases
- Measure tool execution performance
