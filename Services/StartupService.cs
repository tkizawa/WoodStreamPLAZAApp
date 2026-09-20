using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WoodStreamPlaza.Services;

/// <summary>
/// Windowsのスタートアップ登録（自動起動）を管理するサービスクラス
/// </summary>
public static class StartupService
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "WoodStreamPlaza";

    /// <summary>
    /// 現在の実行ファイルパス（.exe）を取得します。
    /// </summary>
    public static string? GetExecutablePath()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath) && processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return processPath;
            }

            string? mainModuleFileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainModuleFileName) && mainModuleFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return mainModuleFileName;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exeName = $"{AppDomain.CurrentDomain.FriendlyName}.exe";
            string candidate = Path.Combine(baseDir, exeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            string fixedCandidate = Path.Combine(baseDir, "WoodStreamPlaza.exe");
            if (File.Exists(fixedCandidate))
            {
                return fixedCandidate;
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"[StartupService] Failed to get executable path: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// スタートアップに自動起動が登録されているかを確認します。
    /// </summary>
    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            if (key == null) return false;

            object? value = key.GetValue(AppRegistryName);
            return value != null;
        }
        catch (Exception ex)
        {
            Logger.Info($"[StartupService] Failed to read registry for auto start: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// スタートアップ自動起動の登録または解除を行います。
    /// </summary>
    /// <param name="enable">自動起動を有効にするか</param>
    /// <param name="startMinimized">起動時に最小化（タスクトレイ格納）するか</param>
    public static bool SetAutoStart(bool enable, bool startMinimized = false)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null)
            {
                Logger.Info("[StartupService] Failed to open Run registry key for writing.");
                return false;
            }

            if (enable)
            {
                string? exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    Logger.Info("[StartupService] Cannot register auto start because executable path was not found.");
                    return false;
                }

                // 最小化起動引数 (--minimized) の付与
                string command = startMinimized
                    ? $"\"{exePath}\" --minimized"
                    : $"\"{exePath}\"";

                key.SetValue(AppRegistryName, command);
                Logger.Info($"[StartupService] Auto start registered successfully: {command}");
                return true;
            }
            else
            {
                if (key.GetValue(AppRegistryName) != null)
                {
                    key.DeleteValue(AppRegistryName, false);
                    Logger.Info("[StartupService] Auto start registry value deleted.");
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"[StartupService] Failed to update auto start registry: {ex.Message}");
            return false;
        }
    }
}
