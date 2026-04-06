// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using Microsoft.Win32;
using System.Text;

namespace ZeroRegistry;

public sealed class RegistryThreatType
{
    public const String ImageHijack = "ImageHijack";
    public const String MaliciousStartup = "MaliciousStartup";
    public const String SuspiciousBootExecute = "SuspiciousBootExecute";
    public const String MaliciousService = "MaliciousService";
    public const String SuspiciousShellExtension = "SuspiciousShellExtension";
    public const String MaliciousAppInit = "MaliciousAppInit";
    public const String SuspiciousUserInit = "SuspiciousUserInit";
    public const String MaliciousWinlogon = "MaliciousWinlogon";
    public const String SuspiciousSafeBoot = "SuspiciousSafeBoot";
    public const String MaliciousContextMenu = "MaliciousContextMenu";
}

public sealed class RegistryScanResult
{
    public String? RegistryPath { get; init; }
    public String? ValueName { get; init; }
    public String? ValueData { get; init; }
    public String? ThreatType { get; init; }
    public String? Description { get; init; }
    public ThreatSeverity Severity { get; init; }
    public String? SuggestedAction { get; init; }
    public DateTime ScanTime { get; init; } = DateTime.UtcNow;
}

public sealed class RegistryScanReport
{
    public Int32 TotalScannedKeys { get; init; }
    public Int32 ThreatsFound { get; init; }
    public Int32 CriticalThreats { get; init; }
    public Int32 HighThreats { get; init; }
    public Int32 MediumThreats { get; init; }
    public Int32 LowThreats { get; init; }
    public IReadOnlyList<RegistryScanResult> Results { get; init; } = [];
    public DateTime ScanTime { get; init; } = DateTime.UtcNow;
    public TimeSpan ScanDuration { get; init; }
    public String? ErrorMessage { get; init; }
}

public enum ThreatSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public sealed class RegistryScanner
{
    private static readonly String[] SuspiciousExtensions = [".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".scr", ".com", ".pif"];
    private static readonly String[] SystemDirectories = [
        @"C:\Windows\System32",
        @"C:\Windows\SysWOW64",
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)"
    ];

    private static readonly String[] TrustedStartupEntries = [
        "SecurityHealth",
        "SecurityHealthService",
        "WindowsSecurityHealthService",
        "OneDrive",
        "MicrosoftEdge",
        "ShellExperienceHost",
        "RuntimeBroker",
        "ApplicationFrameHost",
        "SearchApp",
        "StartMenuExperienceHost",
        "TextInputHost",
        "NcsiUwpApp",
        "SecurityHealthTray",
        "Microsoft.WindowsSecurityHealthProxy"
    ];

    private static readonly String[] KnownMaliciousPatterns = [
        "powershell -e",
        "powershell -enc",
        "powershell -windowstyle hidden",
        "powershell -w hidden",
        "cmd /c",
        "cmd /k",
        "wscript",
        "cscript",
        "mshta",
        "regsvr32 /s",
        "rundll32 javascript",
        "rundll32 vbscript",
        "certutil -urlcache",
        "certutil -f",
        "bitsadmin",
        "msiexec /i http",
        "msiexec /i https",
        "schtasks /create",
        "net user",
        "net localgroup",
        "whoami",
        "systeminfo",
        "taskkill",
        "wmic",
        "downloadstring",
        "downloadfile",
        "invoke-webrequest",
        "invoke-expression",
        "iex",
        "frombase64string",
        "encodedcommand"
    ];

    public RegistryScanReport ScanAll()
    {
        DateTime startTime = DateTime.UtcNow;
        List<RegistryScanResult> results = [];
        Int32 totalScanned = 0;

        try
        {
            ScanImageHijacking(results, ref totalScanned);
            ScanStartupEntries(results, ref totalScanned);
            ScanBootExecute(results, ref totalScanned);
            ScanServices(results, ref totalScanned);
            ScanShellExtensions(results, ref totalScanned);
            ScanAppInitDLLs(results, ref totalScanned);
            ScanUserInit(results, ref totalScanned);
            ScanWinlogon(results, ref totalScanned);
            ScanSafeBoot(results, ref totalScanned);
            ScanContextMenuHandlers(results, ref totalScanned);
        }
        catch (Exception ex)
        {
            return new RegistryScanReport
            {
                ErrorMessage = ex.Message,
                ScanDuration = DateTime.UtcNow - startTime
            };
        }

        TimeSpan duration = DateTime.UtcNow - startTime;
        Int32 criticalThreats = results.Count(r => r.Severity == ThreatSeverity.Critical);
        Int32 highThreats = results.Count(r => r.Severity == ThreatSeverity.High);
        Int32 mediumThreats = results.Count(r => r.Severity == ThreatSeverity.Medium);
        Int32 lowThreats = results.Count(r => r.Severity == ThreatSeverity.Low);

        return new RegistryScanReport
        {
            TotalScannedKeys = totalScanned,
            ThreatsFound = results.Count,
            CriticalThreats = criticalThreats,
            HighThreats = highThreats,
            MediumThreats = mediumThreats,
            LowThreats = lowThreats,
            Results = results,
            ScanDuration = duration
        };
    }

    public async Task<RegistryScanReport> ScanAllAsync(
        Func<RegistryScanResult, Task>? onResult = null,
        CancellationToken cancellationToken = default)
    {
        DateTime startTime = DateTime.UtcNow;
        List<RegistryScanResult> results = [];
        Int32 totalScanned = 0;

        try
        {
            await Task.Run(() => ScanImageHijacking(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanStartupEntries(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanBootExecute(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanServices(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanShellExtensions(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanAppInitDLLs(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanUserInit(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanWinlogon(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanSafeBoot(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);

            await Task.Run(() => ScanContextMenuHandlers(results, ref totalScanned), cancellationToken);
            if (onResult != null)
                foreach (var r in results.Where(r => r.ThreatType != null))
                    await onResult(r);
        }
        catch (OperationCanceledException)
        {
            return new RegistryScanReport
            {
                ErrorMessage = "Scan was cancelled",
                ScanDuration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            return new RegistryScanReport
            {
                ErrorMessage = ex.Message,
                ScanDuration = DateTime.UtcNow - startTime
            };
        }

        TimeSpan duration = DateTime.UtcNow - startTime;
        Int32 criticalThreats = results.Count(r => r.Severity == ThreatSeverity.Critical);
        Int32 highThreats = results.Count(r => r.Severity == ThreatSeverity.High);
        Int32 mediumThreats = results.Count(r => r.Severity == ThreatSeverity.Medium);
        Int32 lowThreats = results.Count(r => r.Severity == ThreatSeverity.Low);

        return new RegistryScanReport
        {
            TotalScannedKeys = totalScanned,
            ThreatsFound = results.Count(r => r.ThreatType != null),
            CriticalThreats = criticalThreats,
            HighThreats = highThreats,
            MediumThreats = mediumThreats,
            LowThreats = lowThreats,
            Results = results.Where(r => r.ThreatType != null).ToList(),
            ScanDuration = duration
        };
    }

    private void ScanImageHijacking(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String[] ifeoPaths = [
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"
        ];

        foreach (String ifeoPath in ifeoPaths)
        {
            using RegistryKey? ifeoKey = OpenRegistryKey(RegistryHive.LocalMachine, ifeoPath);
            if (ifeoKey == null) continue;

            foreach (String subKeyName in ifeoKey.GetSubKeyNames())
            {
                totalScanned++;
                using RegistryKey? subKey = ifeoKey.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                String? debugger = subKey.GetValue("Debugger")?.ToString();
                if (!String.IsNullOrEmpty(debugger))
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{ifeoPath}\{subKeyName}",
                        ValueName = "Debugger",
                        ValueData = debugger,
                        ThreatType = RegistryThreatType.ImageHijack,
                        Description = $"Image hijacking detected: '{subKeyName}' is configured to run under debugger '{debugger}'",
                        Severity = ThreatSeverity.Critical,
                        SuggestedAction = $"Remove the Debugger value from '{subKeyName}' key to restore normal execution"
                    });
                }

                String? globalFlag = subKey.GetValue("GlobalFlag")?.ToString();
                if (!String.IsNullOrEmpty(globalFlag) && globalFlag != "0")
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{ifeoPath}\{subKeyName}",
                        ValueName = "GlobalFlag",
                        ValueData = globalFlag,
                        ThreatType = RegistryThreatType.ImageHijack,
                        Description = $"Suspicious GlobalFlag setting for '{subKeyName}': {globalFlag}",
                        Severity = ThreatSeverity.High,
                        SuggestedAction = "Review and remove suspicious GlobalFlag value"
                    });
                }

                String? verifierDlls = subKey.GetValue("VerifierDlls")?.ToString();
                if (!String.IsNullOrEmpty(verifierDlls))
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{ifeoPath}\{subKeyName}",
                        ValueName = "VerifierDlls",
                        ValueData = verifierDlls,
                        ThreatType = RegistryThreatType.ImageHijack,
                        Description = $"VerifierDlls injection detected for '{subKeyName}': {verifierDlls}",
                        Severity = ThreatSeverity.Critical,
                        SuggestedAction = "Remove VerifierDlls value to prevent DLL injection"
                    });
                }
            }
        }
    }

    private void ScanStartupEntries(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String[] startupPaths = [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SharedTaskScheduler",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellExecuteHooks"
        ];

        RegistryHive[] hives = [RegistryHive.LocalMachine, RegistryHive.CurrentUser];

        foreach (RegistryHive hive in hives)
        {
            foreach (String startupPath in startupPaths)
            {
                using RegistryKey? key = OpenRegistryKey(hive, startupPath);
                if (key == null) continue;

                foreach (String valueName in key.GetValueNames())
                {
                    totalScanned++;
                    String? valueData = key.GetValue(valueName)?.ToString();
                    if (String.IsNullOrEmpty(valueData)) continue;

                    CheckStartupEntry(results, hive, startupPath, valueName, valueData);
                }
            }
        }

        ScanStartupFolder(results, ref totalScanned);
    }

    private void ScanStartupFolder(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String[] startupFolders = [
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup")
        ];

        foreach (String folder in startupFolders)
        {
            if (!Directory.Exists(folder)) continue;

            foreach (String file in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                totalScanned++;
                String extension = Path.GetExtension(file).ToLowerInvariant();

                if (SuspiciousExtensions.Contains(extension) || extension == ".lnk")
                {
                    String fileName = Path.GetFileName(file);
                    Boolean isTrusted = TrustedStartupEntries.Any(t => fileName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!isTrusted)
                    {
                        results.Add(new RegistryScanResult
                        {
                            RegistryPath = file,
                            ValueName = fileName,
                            ValueData = file,
                            ThreatType = RegistryThreatType.MaliciousStartup,
                            Description = $"Suspicious startup item: {fileName}",
                            Severity = ThreatSeverity.Medium,
                            SuggestedAction = "Review and remove if not recognized"
                        });
                    }
                }
            }
        }
    }

    private void CheckStartupEntry(List<RegistryScanResult> results, RegistryHive hive, String path, String name, String data)
    {
        String hiveName = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
        String lowerData = data.ToLowerInvariant();

        foreach (String pattern in KnownMaliciousPatterns)
        {
            if (lowerData.Contains(pattern.ToLowerInvariant()))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"{hiveName}\{path}",
                    ValueName = name,
                    ValueData = data,
                    ThreatType = RegistryThreatType.MaliciousStartup,
                    Description = $"Malicious startup entry detected with pattern '{pattern}'",
                    Severity = ThreatSeverity.Critical,
                    SuggestedAction = "Remove this startup entry immediately"
                });
                return;
            }
        }

        if (IsSuspiciousPath(data))
        {
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"{hiveName}\{path}",
                ValueName = name,
                ValueData = data,
                ThreatType = RegistryThreatType.MaliciousStartup,
                Description = $"Startup entry points to suspicious location: {data}",
                Severity = ThreatSeverity.High,
                SuggestedAction = "Verify the legitimacy of this startup entry"
            });
            return;
        }

        Boolean isTrusted = TrustedStartupEntries.Any(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
        if (!isTrusted && !IsInSystemDirectory(data))
        {
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"{hiveName}\{path}",
                ValueName = name,
                ValueData = data,
                ThreatType = RegistryThreatType.MaliciousStartup,
                Description = $"Non-system startup entry: {name}",
                Severity = ThreatSeverity.Low,
                SuggestedAction = "Review if this startup entry is necessary"
            });
        }
    }

    private void ScanBootExecute(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String bootExecutePath = @"SYSTEM\CurrentControlSet\Control\Session Manager";
        using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, bootExecutePath);
        if (key == null) return;

        String[]? bootExecute = key.GetValue("BootExecute") as String[];
        if (bootExecute == null) return;

        totalScanned++;

        foreach (String entry in bootExecute)
        {
            String lowerEntry = entry.ToLowerInvariant();

            if (lowerEntry.Contains("autocheck autochk *"))
                continue;

            if (!String.IsNullOrWhiteSpace(entry) && !lowerEntry.Contains("autocheck"))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{bootExecutePath}",
                    ValueName = "BootExecute",
                    ValueData = entry,
                    ThreatType = RegistryThreatType.SuspiciousBootExecute,
                    Description = $"Suspicious BootExecute entry: {entry}",
                    Severity = ThreatSeverity.Critical,
                    SuggestedAction = "Remove suspicious BootExecute entry"
                });
            }
        }

        String? setupExecute = key.GetValue("SetupExecute")?.ToString();
        if (!String.IsNullOrEmpty(setupExecute))
        {
            totalScanned++;
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"HKLM\{bootExecutePath}",
                ValueName = "SetupExecute",
                ValueData = setupExecute,
                ThreatType = RegistryThreatType.SuspiciousBootExecute,
                Description = $"SetupExecute detected: {setupExecute}",
                Severity = ThreatSeverity.High,
                SuggestedAction = "Review SetupExecute value for legitimacy"
            });
        }
    }

    private void ScanServices(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String servicesPath = @"SYSTEM\CurrentControlSet\Services";
        using RegistryKey? servicesKey = OpenRegistryKey(RegistryHive.LocalMachine, servicesPath);
        if (servicesKey == null) return;

        foreach (String serviceName in servicesKey.GetSubKeyNames())
        {
            using RegistryKey? serviceKey = servicesKey.OpenSubKey(serviceName);
            if (serviceKey == null) continue;

            totalScanned++;

            String? imagePath = serviceKey.GetValue("ImagePath")?.ToString();
            if (String.IsNullOrEmpty(imagePath)) continue;

            String lowerImagePath = imagePath.ToLowerInvariant();

            foreach (String pattern in KnownMaliciousPatterns)
            {
                if (lowerImagePath.Contains(pattern.ToLowerInvariant()))
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{servicesPath}\{serviceName}",
                        ValueName = "ImagePath",
                        ValueData = imagePath,
                        ThreatType = RegistryThreatType.MaliciousService,
                        Description = $"Malicious service detected with pattern '{pattern}'",
                        Severity = ThreatSeverity.Critical,
                        SuggestedAction = "Disable and remove this service immediately"
                    });
                    break;
                }
            }

            if (IsSuspiciousPath(imagePath))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{servicesPath}\{serviceName}",
                    ValueName = "ImagePath",
                    ValueData = imagePath,
                    ThreatType = RegistryThreatType.MaliciousService,
                    Description = $"Service points to suspicious location: {imagePath}",
                    Severity = ThreatSeverity.High,
                    SuggestedAction = "Verify the legitimacy of this service"
                });
            }

            String? serviceDll = serviceKey.GetValue("ServiceDll")?.ToString();
            if (!String.IsNullOrEmpty(serviceDll) && IsSuspiciousPath(serviceDll))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{servicesPath}\{serviceName}",
                    ValueName = "ServiceDll",
                    ValueData = serviceDll,
                    ThreatType = RegistryThreatType.MaliciousService,
                    Description = $"Service DLL in suspicious location: {serviceDll}",
                    Severity = ThreatSeverity.High,
                    SuggestedAction = "Verify the legitimacy of this service DLL"
                });
            }
        }
    }

    private void ScanShellExtensions(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String[] shellPaths = [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved"
        ];

        foreach (String shellPath in shellPaths)
        {
            using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, shellPath);
            if (key == null) continue;

            foreach (String valueName in key.GetValueNames())
            {
                totalScanned++;
                String? valueData = key.GetValue(valueName)?.ToString();

                if (String.IsNullOrEmpty(valueData)) continue;

                if (valueData.Contains("malware", StringComparison.OrdinalIgnoreCase) ||
                    valueData.Contains("virus", StringComparison.OrdinalIgnoreCase) ||
                    valueData.Contains("trojan", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{shellPath}",
                        ValueName = valueName,
                        ValueData = valueData,
                        ThreatType = RegistryThreatType.SuspiciousShellExtension,
                        Description = $"Suspicious shell extension: {valueData}",
                        Severity = ThreatSeverity.High,
                        SuggestedAction = "Remove this shell extension"
                    });
                }
            }
        }
    }

    private void ScanAppInitDLLs(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String appInitPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows";
        using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, appInitPath);
        if (key == null) return;

        String? appInitDlls = key.GetValue("AppInit_DLLs")?.ToString();
        if (String.IsNullOrEmpty(appInitDlls)) return;

        totalScanned++;

        String[] dlls = appInitDlls.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (String dll in dlls)
        {
            if (String.IsNullOrWhiteSpace(dll)) continue;

            if (IsSuspiciousPath(dll))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{appInitPath}",
                    ValueName = "AppInit_DLLs",
                    ValueData = dll,
                    ThreatType = RegistryThreatType.MaliciousAppInit,
                    Description = $"Suspicious AppInit_DLL: {dll}",
                    Severity = ThreatSeverity.Critical,
                    SuggestedAction = "Remove suspicious AppInit_DLL entry"
                });
            }
            else
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{appInitPath}",
                    ValueName = "AppInit_DLLs",
                    ValueData = dll,
                    ThreatType = RegistryThreatType.MaliciousAppInit,
                    Description = $"AppInit_DLL detected (requires review): {dll}",
                    Severity = ThreatSeverity.Medium,
                    SuggestedAction = "Verify if this AppInit_DLL is legitimate"
                });
            }
        }

        Object? loadAppInitDlls = key.GetValue("LoadAppInit_DLLs");
        if (loadAppInitDlls != null && loadAppInitDlls.ToString() == "1")
        {
            totalScanned++;
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"HKLM\{appInitPath}",
                ValueName = "LoadAppInit_DLLs",
                ValueData = "1",
                ThreatType = RegistryThreatType.MaliciousAppInit,
                Description = "LoadAppInit_DLLs is enabled, allowing DLL injection",
                Severity = ThreatSeverity.Medium,
                SuggestedAction = "Consider disabling if not required"
            });
        }
    }

    private void ScanUserInit(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String winlogonPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, winlogonPath);
        if (key == null) return;

        String? userInit = key.GetValue("Userinit")?.ToString();
        if (String.IsNullOrEmpty(userInit)) return;

        totalScanned++;

        String normalUserInit = @"C:\Windows\system32\userinit.exe,";
        String normalUserInitAlt = @"C:\Windows\System32\userinit.exe,";

        if (!userInit.Equals(normalUserInit, StringComparison.OrdinalIgnoreCase) &&
            !userInit.Equals(normalUserInitAlt, StringComparison.OrdinalIgnoreCase) &&
            !userInit.Equals(@"C:\Windows\system32\userinit.exe", StringComparison.OrdinalIgnoreCase) &&
            !userInit.Equals(@"C:\Windows\System32\userinit.exe", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"HKLM\{winlogonPath}",
                ValueName = "Userinit",
                ValueData = userInit,
                ThreatType = RegistryThreatType.SuspiciousUserInit,
                Description = $"Modified Userinit value detected: {userInit}",
                Severity = ThreatSeverity.Critical,
                SuggestedAction = "Restore Userinit to default value: C:\\Windows\\system32\\userinit.exe,"
            });
        }
    }

    private void ScanWinlogon(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String winlogonPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, winlogonPath);
        if (key == null) return;

        String? shell = key.GetValue("Shell")?.ToString();
        if (!String.IsNullOrEmpty(shell))
        {
            totalScanned++;

            if (!shell.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new RegistryScanResult
                {
                    RegistryPath = $@"HKLM\{winlogonPath}",
                    ValueName = "Shell",
                    ValueData = shell,
                    ThreatType = RegistryThreatType.MaliciousWinlogon,
                    Description = $"Modified Shell value detected: {shell}",
                    Severity = ThreatSeverity.Critical,
                    SuggestedAction = "Restore Shell to default value: explorer.exe"
                });
            }
        }

        String? taskMan = key.GetValue("TaskMan")?.ToString();
        if (!String.IsNullOrEmpty(taskMan))
        {
            totalScanned++;
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"HKLM\{winlogonPath}",
                ValueName = "TaskMan",
                ValueData = taskMan,
                ThreatType = RegistryThreatType.MaliciousWinlogon,
                Description = $"TaskMan value detected: {taskMan}",
                Severity = ThreatSeverity.High,
                SuggestedAction = "Remove TaskMan value"
            });
        }

        String? legalNoticeCaption = key.GetValue("LegalNoticeCaption")?.ToString();
        String? legalNoticeText = key.GetValue("LegalNoticeText")?.ToString();
        if (!String.IsNullOrEmpty(legalNoticeCaption) || !String.IsNullOrEmpty(legalNoticeText))
        {
            totalScanned++;
            results.Add(new RegistryScanResult
            {
                RegistryPath = $@"HKLM\{winlogonPath}",
                ValueName = "LegalNotice",
                ValueData = $"{legalNoticeCaption}: {legalNoticeText}",
                ThreatType = RegistryThreatType.MaliciousWinlogon,
                Description = "Legal notice configured (may be used for social engineering)",
                Severity = ThreatSeverity.Low,
                SuggestedAction = "Review legal notice settings"
            });
        }
    }

    private void ScanSafeBoot(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String safeBootPath = @"SYSTEM\CurrentControlSet\Control\SafeBoot";
        using RegistryKey? safeBootKey = OpenRegistryKey(RegistryHive.LocalMachine, safeBootPath);
        if (safeBootKey == null) return;

        String[] safeBootOptions = ["Minimal", "Network"];
        foreach (String option in safeBootOptions)
        {
            using RegistryKey? optionKey = safeBootKey.OpenSubKey(option);
            if (optionKey == null) continue;

            foreach (String subKeyName in optionKey.GetSubKeyNames())
            {
                totalScanned++;
                using RegistryKey? subKey = optionKey.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                String? defaultValue = subKey.GetValue("")?.ToString();
                if (String.IsNullOrEmpty(defaultValue)) continue;

                if (defaultValue.Contains("malware", StringComparison.OrdinalIgnoreCase) ||
                    defaultValue.Contains("virus", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new RegistryScanResult
                    {
                        RegistryPath = $@"HKLM\{safeBootPath}\{option}\{subKeyName}",
                        ValueName = "(Default)",
                        ValueData = defaultValue,
                        ThreatType = RegistryThreatType.SuspiciousSafeBoot,
                        Description = $"Suspicious SafeBoot entry: {defaultValue}",
                        Severity = ThreatSeverity.High,
                        SuggestedAction = "Remove suspicious SafeBoot entry"
                    });
                }
            }
        }
    }

    private void ScanContextMenuHandlers(List<RegistryScanResult> results, ref Int32 totalScanned)
    {
        String[] contextMenuPaths = [
            @"SOFTWARE\Classes\*\shell",
            @"SOFTWARE\Classes\Directory\shell",
            @"SOFTWARE\Classes\Drive\shell",
            @"SOFTWARE\Classes\Folder\shell"
        ];

        foreach (String contextPath in contextMenuPaths)
        {
            using RegistryKey? key = OpenRegistryKey(RegistryHive.LocalMachine, contextPath);
            if (key == null) continue;

            foreach (String subKeyName in key.GetSubKeyNames())
            {
                totalScanned++;
                using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                using RegistryKey? commandKey = subKey.OpenSubKey("command");
                if (commandKey == null) continue;

                String? command = commandKey.GetValue("")?.ToString();
                if (String.IsNullOrEmpty(command)) continue;

                String lowerCommand = command.ToLowerInvariant();

                foreach (String pattern in KnownMaliciousPatterns)
                {
                    if (lowerCommand.Contains(pattern.ToLowerInvariant()))
                    {
                        results.Add(new RegistryScanResult
                        {
                            RegistryPath = $@"HKLM\{contextPath}\{subKeyName}\command",
                            ValueName = "(Default)",
                            ValueData = command,
                            ThreatType = RegistryThreatType.MaliciousContextMenu,
                            Description = $"Malicious context menu handler with pattern '{pattern}'",
                            Severity = ThreatSeverity.High,
                            SuggestedAction = "Remove this context menu handler"
                        });
                        break;
                    }
                }
            }
        }
    }

    private static RegistryKey? OpenRegistryKey(RegistryHive hive, String path)
    {
        try
        {
            return hive switch
            {
                RegistryHive.LocalMachine => Registry.LocalMachine.OpenSubKey(path),
                RegistryHive.CurrentUser => Registry.CurrentUser.OpenSubKey(path),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static Boolean IsSuspiciousPath(String path)
    {
        String lowerPath = path.ToLowerInvariant();

        String[] suspiciousLocations = [
            @"\appdata\local\temp\",
            @"\appdata\roaming\",
            @"\programdata\",
            @"\users\public\",
            @"\temp\",
            @"\tmp\",
            @"\downloads\",
            @"%temp%",
            @"%appdata%",
            @"%programdata%"
        ];

        foreach (String location in suspiciousLocations)
        {
            if (lowerPath.Contains(location))
                return true;
        }

        if (lowerPath.StartsWith(@"\") && !lowerPath.StartsWith(@"\\"))
            return true;

        if (lowerPath.Contains("http://") || lowerPath.Contains("https://"))
            return true;

        return false;
    }

    private static Boolean IsInSystemDirectory(String path)
    {
        String lowerPath = path.ToLowerInvariant();

        foreach (String dir in SystemDirectories)
        {
            if (lowerPath.Contains(dir.ToLowerInvariant()))
                return true;
        }

        return false;
    }
}
