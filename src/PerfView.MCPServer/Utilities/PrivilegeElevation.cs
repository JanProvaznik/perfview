using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

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
    /// Executes a command in an elevated helper process.
    /// On Windows, this triggers a UAC prompt. On Unix, uses sudo.
    /// </summary>
    /// <param name="helperCommand">Command to pass to the helper (e.g., "collect-cpu")</param>
    /// <param name="arguments">Arguments for the command</param>
    /// <returns>Tuple of (exitCode, stdout, stderr)</returns>
    public static async Task<(int exitCode, string stdout, string stderr)> ExecuteElevatedAsync(
        string helperCommand, params string[] arguments)
    {
        var helperPath = FindHelperExecutable();
        if (helperPath == null)
        {
            return (1, "", "ERROR: TraceHelper executable not found. Build the solution first.");
        }

        var allArgs = new[] { helperCommand }.Concat(arguments).ToArray();
        var argsString = string.Join(" ", allArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, use runas to trigger UAC
            startInfo.FileName = helperPath;
            startInfo.Arguments = argsString;
            startInfo.Verb = "runas"; // This triggers UAC
            startInfo.UseShellExecute = true; // Required for runas
            startInfo.RedirectStandardOutput = false; // Can't redirect with UseShellExecute
            startInfo.RedirectStandardError = false;
        }
        else
        {
            // On Unix, use sudo
            startInfo.FileName = "sudo";
            startInfo.Arguments = $"{helperPath} {argsString}";
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (1, "", "ERROR: Failed to start elevated process");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows with UAC, we can't capture output, so wait and check file
                await process.WaitForExitAsync();
                return (process.ExitCode, "", "");
            }
            else
            {
                // On Unix with sudo, we can capture output
                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return (process.ExitCode, stdout, stderr);
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            return (1, "", "ERROR: User cancelled elevation prompt");
        }
        catch (Exception ex)
        {
            return (1, "", $"ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the TraceHelper executable.
    /// </summary>
    private static string? FindHelperExecutable()
    {
        var currentDir = AppContext.BaseDirectory;
        var helperName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "PerfView.MCPServer.TraceHelper.exe"
            : "PerfView.MCPServer.TraceHelper";

        // Check in current directory
        var helperPath = Path.Combine(currentDir, helperName);
        if (File.Exists(helperPath))
            return helperPath;

        // Check in TraceHelper subdirectory
        helperPath = Path.Combine(currentDir, "TraceHelper", helperName);
        if (File.Exists(helperPath))
            return helperPath;

        // Check in sibling directory (common in dev builds)
        var parentDir = Directory.GetParent(currentDir)?.FullName;
        if (parentDir != null)
        {
            helperPath = Path.Combine(parentDir, "PerfView.MCPServer.TraceHelper", helperName);
            if (File.Exists(helperPath))
                return helperPath;
        }

        return null;
    }
}
