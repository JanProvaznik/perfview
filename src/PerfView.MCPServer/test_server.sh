#!/bin/bash

# Test script to verify MCP server is working
# This simulates an MCP client sending requests

echo "Testing PerfView MCP Server..." >&2

# Start the server in the background
dotnet run --project /home/runner/work/perfview/perfview/src/PerfView.MCPServer/PerfView.MCPServer.csproj &
SERVER_PID=$!

# Give it time to start
sleep 3

# Send MCP initialize request
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0"}}}' | tee /dev/stderr

# Wait a bit
sleep 1

# Send tools/list request
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | tee /dev/stderr

# Wait for responses
sleep 2

# Clean up
kill $SERVER_PID 2>/dev/null

echo "" >&2
echo "Test complete" >&2
