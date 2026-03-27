// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using SouXiaoAVE.DataExtraction;
using SouXiaoAVE.DataProcessing;
using SouXiaoAVE.Interfaces;
using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;
using SouXiaoAVE.Service;
using SouXiaoAVE.Utils;

namespace SouXiaoAVE;

internal class Entry
{
    static async Task Main()
    {
        Console.WriteLine("=== SouXiaoAVE TCP Connection Test ===");
        Console.WriteLine();

        Console.WriteLine("Starting Service Host...");
        ScanServiceHost serviceHost = ScanServiceHost.Instance;

        Boolean serviceStarted = serviceHost.Start();
        Console.WriteLine($"Service Started: {serviceStarted}");
        Console.WriteLine($"Service Port: {serviceHost.Port}");
        Console.WriteLine();

        Console.WriteLine("Starting Linker Service...");
        ServiceResult startResult = Linker.Linker.StartService();
        Console.WriteLine($"Linker Service Start Result: {startResult}");

        await Task.Delay(500);

        Console.WriteLine("Attempting to connect Linker to Service...");
        Linker.Linker? linker = await Linker.Linker.LinkServiceAsync("localhost", 9527);

        if (linker is null)
        {
            Console.WriteLine("Failed to connect to service!");
            return;
        }

        Console.WriteLine($"Linker Connected: {linker.IsConnected}");
        Console.WriteLine();

        String notepadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");

        if (!File.Exists(notepadPath))
        {
            Console.WriteLine($"Notepad not found at: {notepadPath}");
            return;
        }

        Console.WriteLine($"Test File: {notepadPath}");
        Console.WriteLine();

        Console.WriteLine("=== Test 1: Single File Scan (Quick Mode) ===");
        SXTask fileTask = linker.CreateTask(notepadPath, SXType.File);
        Console.WriteLine($"Created Task: {fileTask.TaskId}");
        Console.WriteLine($"Task Type: {fileTask.Type}");

        Object sendResult = await linker.SendTaskAsync([fileTask]);

        if (sendResult is Report quickReport)
        {
            Console.WriteLine($"Quick Scan Result:");
            Console.WriteLine($"  Task ID: {quickReport.TaskId}");
            Console.WriteLine($"  File Path: {quickReport.FilePath}");
            Console.WriteLine($"  Is Malicious: {quickReport.IsMalicious}");
            Console.WriteLine($"  Confidence: {quickReport.Confidence:P}");
            Console.WriteLine($"  Is Valid: {quickReport.IsValid}");
        }
        else if (sendResult is TaskID[] taskIds)
        {
            Console.WriteLine($"Tasks submitted: {taskIds.Length}");
            Console.WriteLine($"Task ID: {taskIds[0]}");

            Console.WriteLine("Waiting for task completion...");
            Report report = await linker.WaitTaskAsync(taskIds[0]);
            Console.WriteLine($"Report received:");
            Console.WriteLine($"  Is Malicious: {report.IsMalicious}");
            Console.WriteLine($"  Confidence: {report.Confidence:P}");
        }
        else if (sendResult is ServiceResult errorResult)
        {
            Console.WriteLine($"Error: {errorResult}");
        }
        Console.WriteLine();

        Console.WriteLine("=== Test 2: Multiple Files Scan ===");
        SXTask task1 = linker.CreateTask(notepadPath, SXType.File);
        SXTask task2 = linker.CreateTask(notepadPath, SXType.File);

        Object multiResult = await linker.SendTaskAsync([task1, task2]);

        if (multiResult is TaskID[] multiTaskIds)
        {
            Console.WriteLine($"Multiple tasks submitted: {multiTaskIds.Length}");

            foreach (TaskID tid in multiTaskIds)
            {
                SXTaskState state = await linker.GetTaskStateAsync(tid);
                Console.WriteLine($"  Task {tid}: State = {state}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("=== Test 3: Get Task State ===");
        SXTaskState taskState = await linker.GetTaskStateAsync(fileTask.TaskId);
        Console.WriteLine($"Task State: {taskState}");
        Console.WriteLine();

        Console.WriteLine("=== Test 4: Get Process Rate ===");
        Double progress = await linker.GetProcessRateAsync(fileTask.TaskId);
        Console.WriteLine($"Progress: {progress:P}");
        Console.WriteLine();

        Console.WriteLine("=== Test 5: Get Report ===");
        Report? existingReport = await linker.GetReportAsync(fileTask.TaskId);
        if (existingReport is not null)
        {
            Console.WriteLine($"Report found:");
            Console.WriteLine($"  File: {existingReport.FilePath}");
            Console.WriteLine($"  Malicious: {existingReport.IsMalicious}");
        }
        else
        {
            Console.WriteLine("No report available yet");
        }
        Console.WriteLine();

        Console.WriteLine("=== Test 6: Folder Scan ===");
        String windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        SXTask folderTask = linker.CreateTask(windowsPath, SXType.Folder);
        Console.WriteLine($"Created Folder Task: {folderTask.TaskId}");

        Object folderResult = await linker.SendTaskAsync([folderTask]);
        if (folderResult is TaskID[] folderTaskIds)
        {
            Console.WriteLine($"Folder task submitted: {folderTaskIds[0]}");

            for (Int32 i = 0; i < 3; i++)
            {
                await Task.Delay(100);
                Double rate = await linker.GetProcessRateAsync(folderTaskIds[0]);
                SXTaskState state = await linker.GetTaskStateAsync(folderTaskIds[0]);
                Console.WriteLine($"  Progress: {rate:P}, State: {state}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("=== Test 7: Stop/Restart Task ===");
        ServiceResult stopResult = await linker.StopTaskAsync(folderTask.TaskId);
        Console.WriteLine($"Stop Task Result: {stopResult}");

        SXTaskState stoppedState = await linker.GetTaskStateAsync(folderTask.TaskId);
        Console.WriteLine($"State after stop: {stoppedState}");

        ServiceResult restartResult = await linker.RestartTaskAsync(folderTask.TaskId);
        Console.WriteLine($"Restart Task Result: {restartResult}");
        Console.WriteLine();

        Console.WriteLine("=== Test 8: Stop All / Release All ===");
        ServiceResult stopAllResult = await linker.StopAllAsync();
        Console.WriteLine($"Stop All Result: {stopAllResult}");

        ServiceResult releaseAllResult = await linker.ReleaseAllAsync();
        Console.WriteLine($"Release All Result: {releaseAllResult}");
        Console.WriteLine();

        Console.WriteLine("=== Test 9: Disconnect ===");
        ServiceResult disconnectResult = await linker.DisconnectAsync();
        Console.WriteLine($"Disconnect Result: {disconnectResult}");
        Console.WriteLine($"Linker Connected: {linker.IsConnected}");
        Console.WriteLine();

        Console.WriteLine("Stopping Service...");
        ServiceResult stopServiceResult = Linker.Linker.StopService();
        Console.WriteLine($"Stop Service Result: {stopServiceResult}");

        serviceHost.Stop();
        Console.WriteLine("Service Host Stopped");

        linker.Dispose();
        serviceHost.Dispose();

        Console.WriteLine();
        Console.WriteLine("All tests completed successfully!");
    }
}
