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

        // On Windows with UAC, we can't capture stdout/stderr, so use a temp file for communication
        string? resultFilePath = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            resultFilePath = Path.Combine(Path.GetTempPath(), $"perfview_helper_{Guid.NewGuid():N}.json");
        }

        var allArgs = new[] { helperCommand }.Concat(arguments).ToArray();
        
        // Add result file path as the last argument on Windows
        if (resultFilePath != null)
        {
            allArgs = allArgs.Append(resultFilePath).ToArray();
        }
        
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
                // On Windows with UAC, we can't capture output, so read from result file
                await process.WaitForExitAsync();
                
                // Read result file if it exists
                if (resultFilePath != null && File.Exists(resultFilePath))
                {
                    try
                    {
                        var resultContent = await File.ReadAllTextAsync(resultFilePath);
                        File.Delete(resultFilePath); // Clean up
                        return (process.ExitCode, resultContent, "");
                    }
                    catch
                    {
                        // If we can't read the file, return exit code only
                        return (process.ExitCode, "", "Unable to read helper result file");
                    }
                }
                
                return (process.ExitCode, "", process.ExitCode != 0 ? "Elevated process failed (no output captured)" : "");
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
            if (resultFilePath != null && File.Exists(resultFilePath))
            {
                try { File.Delete(resultFilePath); } catch { }
            }
            return (1, "", "ERROR: User cancelled elevation prompt");
        }
        catch (Exception ex)
        {
            if (resultFilePath != null && File.Exists(resultFilePath))
            {
                try { File.Delete(resultFilePath); } catch { }
            }
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
