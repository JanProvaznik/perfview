using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace PerfView.MCPServer.Utilities;

/// <summary>
/// Utilities for checking and requesting administrator privileges.
/// </summary>
public static class PrivilegeElevation
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    public static bool IsAdministrator()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On non-Windows platforms, check for root/sudo
            try
            {
                return Environment.UserName == "root" || Environment.GetEnvironmentVariable("SUDO_USER") != null;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly message about how to run with elevated privileges.
    /// </summary>
    public static string GetElevationInstructions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"Administrator privileges are required for this operation.

To run with elevated permissions:
1. Close your MCP client (e.g., Claude Desktop)
2. Open a new terminal as Administrator:
   - Right-click on PowerShell or Command Prompt
   - Select 'Run as Administrator'
3. Navigate to the PerfView.MCPServer directory
4. Run: dotnet run
5. Reconnect your MCP client

Alternative (just-in-time elevation - experimental):
This operation could benefit from UAC-based privilege escalation, but current
MCP protocol limitations require the entire server to run with elevated permissions.
Future SDK versions may support spawning elevated child processes.";
        }
        else
        {
            return @"Root/sudo privileges are required for this operation.

To run with elevated permissions:
1. Close your MCP client
2. Open a new terminal
3. Navigate to the PerfView.MCPServer directory
4. Run: sudo dotnet run
5. Reconnect your MCP client";
        }
    }
}
