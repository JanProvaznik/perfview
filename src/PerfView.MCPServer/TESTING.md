# Testing the PerfView MCP Server

## Verification Steps

### 1. Build Verification

```bash
cd src/PerfView.MCPServer
dotnet build
```

Expected: Build succeeds with 0 errors and 0 warnings.

### 2. Server Start Test

```bash
dotnet run
```

Expected: Server starts and waits for stdin input (MCP protocol communication).
Press Ctrl+C to stop.

### 3. MCP Protocol Test

The server implements the MCP protocol over stdio. Here's how to test with a simple MCP client:

#### List Available Tools

Create a file `test_list_tools.json`:
```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
```

Test:
```bash
cat test_list_tools.json | dotnet run 2>&1 | grep -A 20 "collect_cpu_trace"
```

Expected: You should see tool definitions including `collect_cpu_trace`, `analyze_cpu_hotspots`, `query_events`, and `get_trace_stats`.

### 4. Integration Test with Claude Desktop

To test with Claude Desktop:

1. Configure `claude_desktop_config.json` (see README_USAGE.md)
2. Restart Claude Desktop
3. Start a conversation and ask: "What tools do you have available for performance analysis?"
4. Claude should list the PerfView MCP tools

### 5. Functional Test (Requires Admin on Windows)

To test actual trace collection:

```bash
# Note: Requires administrator privileges on Windows
dotnet run
```

Then send (via stdin):
```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"collect_cpu_trace","arguments":{"durationSeconds":5,"outputPath":"./test_trace.etl"}}}
```

Expected: After 5 seconds, you should get a success response and see `test_trace.etl` created.

### 6. Trace Analysis Test

After collecting a trace, test analysis:

```json
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_trace_stats","arguments":{"tracePath":"./test_trace.etl"}}}
```

Expected: Statistics about the trace file (size, duration, event counts).

## Automated Testing

The TestTools project validates tool method signatures and JSON serialization:

```bash
cd ../PerfView.MCPServer.TestTools
dotnet run
```

Expected: All tools execute successfully with placeholder data.

## Common Issues

### "Administrator privileges required"

**Solution**: Run with elevated permissions:
- Windows: Run PowerShell/CMD as Administrator
- Linux: Use `sudo` (though kernel tracing is Windows-only)

### "ModelContextProtocol package not found"

**Solution**: Restore packages:
```bash
dotnet restore
```

### Server doesn't respond

**Symptoms**: No output after sending MCP requests

**Possible causes**:
1. Incorrect JSON-RPC format
2. Missing newline after JSON
3. Server not reading from stdin

**Solution**: Ensure each JSON request is on a single line followed by newline.

### Trace file empty or corrupted

**Symptoms**: Analysis tools return "No CPU samples found"

**Possible causes**:
1. Insufficient permissions for kernel tracing
2. Trace duration too short
3. No activity during trace collection

**Solution**: 
- Run with admin privileges
- Increase trace duration
- Ensure target process is active

## Performance Benchmarks

### Trace Collection
- Small trace (5s): ~5-10 MB
- Medium trace (30s): ~30-50 MB  
- Large trace (60s): ~60-100 MB

### Analysis Performance
- Small trace: < 1 second
- Medium trace: 1-3 seconds
- Large trace: 3-10 seconds

## Next Steps

After successful testing:

1. **Configure with AI Assistant**: Set up Claude Desktop or other MCP client
2. **Test Workflows**: Try the example workflows from README_USAGE.md
3. **Custom Scenarios**: Adapt for your specific performance investigation needs
4. **Provide Feedback**: Report issues or suggestions on GitHub

## Advanced Testing

### Custom MCP Client

You can create a custom MCP client in any language. Example in Python:

```python
import subprocess
import json
import sys

# Start the MCP server
process = subprocess.Popen(
    ['dotnet', 'run', '--project', 'PerfView.MCPServer.csproj'],
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,
    text=True
)

# Send initialize
init_request = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {"name": "test-client", "version": "1.0"}
    }
}
process.stdin.write(json.dumps(init_request) + '\n')
process.stdin.flush()

# Read response
response = process.stdout.readline()
print("Initialize response:", response)

# List tools
list_request = {
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
}
process.stdin.write(json.dumps(list_request) + '\n')
process.stdin.flush()

response = process.stdout.readline()
print("Tools list:", response)

# Cleanup
process.terminate()
```

## Troubleshooting Tips

1. **Check stderr**: All server logs go to stderr, leaving stdout for MCP protocol
2. **Validate JSON**: Use a JSON validator to check request format
3. **Single line**: Each JSON request must be on a single line
4. **Newline terminated**: Each request must end with `\n`
5. **Sequential IDs**: Use sequential IDs for requests

## Success Criteria

✅ Server builds without errors  
✅ Server starts and waits for input  
✅ Tools are listed via MCP protocol  
✅ Trace collection works (with admin rights)  
✅ Trace analysis returns results  
✅ Integration with Claude Desktop succeeds  

## Support

For issues:
- Check this testing guide
- Review README_USAGE.md for usage details
- See GitHub issues for known problems
- Post new issues with error logs and steps to reproduce
