// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Diagnostics;
using PeNet;

namespace ZeroProcess;

public sealed class ProcessScanResult
{
    public Int32 ProcessId { get; init; }
    public String? ProcessName { get; init; }
    public String? FilePath { get; init; }
    public String? Label { get; init; }
    public Double Confidence { get; init; }
    public Double Score { get; init; }
    public Boolean IsMalicious { get; init; }
    public String? ErrorMessage { get; init; }
}

public sealed class ProcessScanReport
{
    public Int32 TotalProcesses { get; init; }
    public Int32 ScannedProcesses { get; init; }
    public Int32 MaliciousProcesses { get; init; }
    public Int32 BenignProcesses { get; init; }
    public Int32 SkippedProcesses { get; init; }
    public Int32 ErrorProcesses { get; init; }
    public IReadOnlyList<ProcessScanResult> Results { get; init; } = [];
    public DateTime ScanTime { get; init; } = DateTime.UtcNow;
    public String? ErrorMessage { get; init; }
}

public interface IProcessMalwareDetector
{
    Task<(String Label, Double Confidence, Double Score)> DetectAsync(Byte[] peBytes);
}

public sealed class ProcessScanner : IDisposable
{
    private readonly IProcessMalwareDetector? _detector;
    private Boolean _disposed;

    public ProcessScanner() { }

    public ProcessScanner(IProcessMalwareDetector detector)
    {
        _detector = detector;
    }

    public async Task<ProcessScanReport> ScanAllProcessesAsync(
        Func<ProcessScanResult, Task>? onResult = null,
        CancellationToken cancellationToken = default)
    {
        List<ProcessScanResult> results = [];
        Int32 totalProcesses = 0;
        Int32 scannedProcesses = 0;
        Int32 maliciousProcesses = 0;
        Int32 benignProcesses = 0;
        Int32 skippedProcesses = 0;
        Int32 errorProcesses = 0;

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            return new ProcessScanReport
            {
                TotalProcesses = 0,
                ErrorMessage = $"Failed to get process list: {ex.Message}"
            };
        }

        totalProcesses = processes.Length;

        foreach (Process process in processes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            ProcessScanResult result = await ScanProcessAsync(process);
            results.Add(result);

            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                errorProcesses++;
            }
            else if (result.Label == "Malicious")
            {
                maliciousProcesses++;
                scannedProcesses++;
            }
            else if (result.Label == "Benign")
            {
                benignProcesses++;
                scannedProcesses++;
            }
            else
            {
                skippedProcesses++;
            }

            if (onResult is not null)
            {
                await onResult(result);
            }
        }

        foreach (Process process in processes)
        {
            process.Dispose();
        }

        return new ProcessScanReport
        {
            TotalProcesses = totalProcesses,
            ScannedProcesses = scannedProcesses,
            MaliciousProcesses = maliciousProcesses,
            BenignProcesses = benignProcesses,
            SkippedProcesses = skippedProcesses,
            ErrorProcesses = errorProcesses,
            Results = results
        };
    }

    public async Task<ProcessScanResult> ScanProcessAsync(Process process)
    {
        try
        {
            String? filePath = GetProcessFilePath(process);

            if (String.IsNullOrEmpty(filePath))
            {
                return new ProcessScanResult
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    ErrorMessage = "No file path available"
                };
            }

            if (!File.Exists(filePath))
            {
                return new ProcessScanResult
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    FilePath = filePath,
                    ErrorMessage = "File not found"
                };
            }

            Byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            if (!IsValidPeFile(fileBytes))
            {
                return new ProcessScanResult
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    FilePath = filePath,
                    ErrorMessage = "Not a valid PE file"
                };
            }

            if (_detector is null)
            {
                return new ProcessScanResult
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    FilePath = filePath,
                    Label = "Unknown",
                    ErrorMessage = "No detector configured"
                };
            }

            (String label, Double confidence, Double score) = await _detector.DetectAsync(fileBytes);

            Boolean isMalicious = label == "Malicious";

            return new ProcessScanResult
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                FilePath = filePath,
                Label = label,
                Confidence = confidence,
                Score = score,
                IsMalicious = isMalicious
            };
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ProcessScanResult
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                ErrorMessage = "Access denied"
            };
        }
        catch (Exception ex)
        {
            return new ProcessScanResult
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ProcessScanResult> ScanProcessByIdAsync(Int32 processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            return await ScanProcessAsync(process);
        }
        catch (ArgumentException)
        {
            return new ProcessScanResult
            {
                ProcessId = processId,
                ErrorMessage = "Process not found"
            };
        }
        catch (Exception ex)
        {
            return new ProcessScanResult
            {
                ProcessId = processId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ProcessScanResult> ScanProcessByNameAsync(String processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                return new ProcessScanResult
                {
                    ProcessName = processName,
                    ErrorMessage = "Process not found"
                };
            }

            ProcessScanResult result = await ScanProcessAsync(processes[0]);

            foreach (Process p in processes)
            {
                p.Dispose();
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ProcessScanResult
            {
                ProcessName = processName,
                ErrorMessage = ex.Message
            };
        }
    }

    private static String? GetProcessFilePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static Boolean IsValidPeFile(Byte[] data)
    {
        if (data.Length < 2)
            return false;

        return data[0] == 0x4D && data[1] == 0x5A;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
