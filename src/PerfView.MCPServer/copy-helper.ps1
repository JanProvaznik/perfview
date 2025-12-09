# PowerShell script to copy TraceHelper after build
$helperPath = Join-Path $PSScriptRoot "..\PerfView.MCPServer.TraceHelper\bin\Debug\net8.0\PerfView.MCPServer.TraceHelper.exe"
$targetPath = Join-Path $PSScriptRoot "bin\Debug\net8.0\PerfView.MCPServer.TraceHelper.exe"

if (Test-Path $helperPath) {
    Copy-Item -Path $helperPath -Destination $targetPath -Force
    Write-Host "Copied TraceHelper to: $targetPath"
} else {
    Write-Host "Warning: TraceHelper not found at: $helperPath"
    Write-Host "Make sure to build PerfView.MCPServer.TraceHelper project first"
}
