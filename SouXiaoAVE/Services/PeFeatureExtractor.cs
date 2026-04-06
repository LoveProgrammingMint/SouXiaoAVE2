// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using Gee.External.Capstone;
using Gee.External.Capstone.X86;
using PeNet;
using PeNet.Header.Pe;

using SouXiaoAVE.Models;
using SouXiaoAVE.Utils;

namespace SouXiaoAVE.Services;

public sealed class PeFeatureExtractor
{
    private const Int32 MaxSections = 20;
    private const Int32 SegmentCount = 32;

    private static readonly String[] KeyDlls =
    [
        "kernel32.dll", "ntdll.dll", "user32.dll", "advapi32.dll", "ws2_32.dll",
        "wininet.dll", "shell32.dll", "ole32.dll", "oleaut32.dll", "comctl32.dll",
        "comdlg32.dll", "gdi32.dll", "opengl32.dll", "msvcrt.dll", "crypt32.dll",
        "winsock.dll", "urlmon.dll", "shlwapi.dll", "version.dll", "setupapi.dll",
        "secur32.dll", "netapi32.dll", "dnsapi.dll", "winhttp.dll", "msasn1.dll",
        "wintrust.dll", "imagehlp.dll", "dbghelp.dll", "psapi.dll", "imm32.dll"
    ];

    private static readonly String[] KeyApis =
    [
        "CreateRemoteThread", "WriteProcessMemory", "ReadProcessMemory", "VirtualAllocEx", "VirtualProtectEx",
        "OpenProcess", "QueueUserAPC", "SetThreadContext", "NtCreateThreadEx", "RtlCreateUserThread",
        "CreateFile", "WriteFile", "DeleteFile", "MoveFile", "CopyFile",
        "RegCreateKeyEx", "RegSetValueEx", "ShellExecute", "WinExec", "CreateService",
        "WSAStartup", "socket", "connect", "send", "recv",
        "URLDownloadToFile", "InternetOpen", "InternetConnect", "HttpOpenRequest", "WinHttpOpen",
        "IsDebuggerPresent", "CheckRemoteDebuggerPresent", "NtQueryInformationProcess", "OutputDebugString", "GetTickCount",
        "QueryPerformanceCounter", "SetUnhandledExceptionFilter", "AddVectoredExceptionHandler",
        "CryptAcquireContext", "CryptEncrypt", "CryptDecrypt", "RSAEncrypt", "MD5Init", "SHA1Init",
        "LoadLibrary", "GetProcAddress", "VirtualProtect", "VirtualAlloc", "HeapCreate",
        "EnumWindows", "GetAsyncKeyState", "SetWindowsHookEx"
    ];

    private static readonly String[] KeySubstrings =
    [
        "cmd.exe", "powershell", "rundll32", "regsvr32", "sc.exe",
        "net.exe", "schtasks", "wmic", "mshta", "certutil",
        "bitsadmin", "windows defender", "disable", "hide", "delete",
        "-enc", "-e", "Invoke-", "DownloadString", "FromBase64",
        "CreateObject", "WScript.Shell", "Shell.Application", "HKEY_LOCAL_MACHINE", "SOFTWARE\\Microsoft",
        "CurrentVersion\\Run", "%TEMP%", "\\\\", ":", "|"
    ];

    private static readonly String[] SuspiciousSectionNames =
    [
        "UPX0", "UPX1", ".aspack", ".PACK", ".RLPack",
        ".MPress", ".yP", ".NSP", ".themida", ".enigma",
        ".vmp0", ".vmp1", ".CODE", ".DATA", ".itext"
    ];

    private Byte[] _fileBytes = [];
    private PeFile? _peFile;

    public FeatureVector Extract(String filePath)
    {
        _fileBytes = File.ReadAllBytes(filePath);
        _peFile = new PeFile(filePath);

        FeatureVector features = new();
        Int32 offset = 0;

        offset = ExtractGlobalStatistics(features, offset);
        offset = ExtractOptionalHeaderFeatures(features, offset);
        offset = ExtractSectionFeatures(features, offset);
        offset = ExtractImportFeatures(features, offset);
        offset = ExtractExportFeatures(features, offset);
        offset = ExtractResourceFeatures(features, offset);
        offset = ExtractStringFeatures(features, offset);
        offset = ExtractEntryPointFeatures(features, offset);
        offset = ExtractRelocationTlsFeatures(features, offset);
        offset = ExtractByteStatistics(features, offset);
        offset = ExtractSupplementaryFeatures(features, offset);

        return features;
    }

    public FeatureVector Extract(Byte[] fileBytes)
    {
        _fileBytes = fileBytes;
        _peFile = new PeFile(fileBytes);

        FeatureVector features = new();
        Int32 offset = 0;

        offset = ExtractGlobalStatistics(features, offset);
        offset = ExtractOptionalHeaderFeatures(features, offset);
        offset = ExtractSectionFeatures(features, offset);
        offset = ExtractImportFeatures(features, offset);
        offset = ExtractExportFeatures(features, offset);
        offset = ExtractResourceFeatures(features, offset);
        offset = ExtractStringFeatures(features, offset);
        offset = ExtractEntryPointFeatures(features, offset);
        offset = ExtractRelocationTlsFeatures(features, offset);
        offset = ExtractByteStatistics(features, offset);
        offset = ExtractSupplementaryFeatures(features, offset);

        return features;
    }

    private Int32 ExtractGlobalStatistics(FeatureVector features, Int32 offset)
    {
        features[offset++] = _fileBytes.Length;
        features[offset++] = EntropyCalculator.Calculate(_fileBytes);
        features[offset++] = CalculateCompressionRatio(_fileBytes);

        Int32 segmentSize = (Int32)Math.Ceiling(_fileBytes.Length / (Double)SegmentCount);
        for (Int32 i = 0; i < SegmentCount; i++)
        {
            Int32 start = i * segmentSize;
            Int32 end = Math.Min(start + segmentSize, _fileBytes.Length);
            if (start < _fileBytes.Length)
            {
                ReadOnlySpan<Byte> segment = new(_fileBytes, start, end - start);
                features[offset + i] = EntropyCalculator.Calculate(segment);
            }
            else
            {
                features[offset + i] = 0;
            }
        }

        return offset + SegmentCount;
    }

    private Int32 ExtractOptionalHeaderFeatures(FeatureVector features, Int32 offset)
    {
        ImageOptionalHeader? optHeader = _peFile?.ImageNtHeaders?.OptionalHeader;
        if (optHeader is null)
        {
            for (Int32 i = 0; i < 20; i++)
                features[offset + i] = 0;
            return offset + 20;
        }

        features[offset++] = optHeader.MajorLinkerVersion;
        features[offset++] = optHeader.MinorLinkerVersion;
        features[offset++] = optHeader.SizeOfCode;
        features[offset++] = optHeader.SizeOfInitializedData;
        features[offset++] = optHeader.SizeOfUninitializedData;
        features[offset++] = optHeader.AddressOfEntryPoint;
        features[offset++] = optHeader.BaseOfCode;
        features[offset++] = optHeader.ImageBase;
        features[offset++] = optHeader.SectionAlignment;
        features[offset++] = optHeader.FileAlignment;
        features[offset++] = optHeader.MajorOperatingSystemVersion;
        features[offset++] = optHeader.MinorOperatingSystemVersion;
        features[offset++] = optHeader.MajorSubsystemVersion;
        features[offset++] = optHeader.MinorSubsystemVersion;
        features[offset++] = optHeader.SizeOfImage;
        features[offset++] = optHeader.SizeOfHeaders;
        features[offset++] = (Double)optHeader.Subsystem;
        features[offset++] = (Double)optHeader.DllCharacteristics;
        features[offset++] = optHeader.SizeOfStackReserve;

        return offset;
    }

    private Int32 ExtractSectionFeatures(FeatureVector features, Int32 offset)
    {
        ImageSectionHeader[]? sections = _peFile?.ImageSectionHeaders;
        Int32 sectionCount = Math.Min(sections?.Length ?? 0, MaxSections);

        for (Int32 i = 0; i < MaxSections; i++)
        {
            if (i < sectionCount && sections is not null)
            {
                ImageSectionHeader sec = sections[i];
                features[offset++] = MurmurHash3.HashString(sec.Name) / (Double)UInt32.MaxValue;
                features[offset++] = sec.VirtualSize;
                features[offset++] = sec.SizeOfRawData;
                features[offset++] = GetSectionEntropy(sec);
                features[offset++] = sec.Characteristics.HasFlag(ScnCharacteristicsType.MemExecute) ? 1 : 0;
                features[offset++] = sec.Characteristics.HasFlag(ScnCharacteristicsType.MemWrite) ? 1 : 0;
                features[offset++] = sec.Characteristics.HasFlag(ScnCharacteristicsType.CntCode) ? 1 : 0;
                features[offset++] = sec.Characteristics.HasFlag(ScnCharacteristicsType.CntInitializedData) ? 1 : 0;
                features[offset++] = IsStandardSectionName(sec.Name) ? 1 : 0;
                features[offset++] = sec.PointerToRawData < _fileBytes.Length ? 1 : 0;
                features[offset++] = ((UInt32)sec.Characteristics) % 256;
                features[offset++] = CalculateStringDensity(sec);
            }
            else
            {
                for (Int32 j = 0; j < 12; j++)
                    features[offset++] = 0;
            }
        }

        return offset;
    }

    private Int32 ExtractImportFeatures(FeatureVector features, Int32 offset)
    {
        ImportFunction[]? imports = _peFile?.ImportedFunctions;
        Dictionary<String, Int32> dllCounts = new(StringComparer.OrdinalIgnoreCase);
        foreach (String dll in KeyDlls)
            dllCounts[dll] = 0;

        if (imports is not null)
        {
            foreach (ImportFunction imp in imports)
            {
                String dll = imp.DLL?.ToLowerInvariant() ?? "";
                foreach (String keyDll in KeyDlls)
                {
                    if (dll.Contains(keyDll.Replace(".dll", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        dllCounts[keyDll]++;
                        break;
                    }
                }
            }
        }

        foreach (String dll in KeyDlls)
            features[offset++] = dllCounts[dll];

        HashSet<String> apiSet = new(StringComparer.OrdinalIgnoreCase);
        if (imports is not null)
        {
            foreach (ImportFunction imp in imports)
            {
                if (!String.IsNullOrEmpty(imp.Name))
                    apiSet.Add(imp.Name);
            }
        }

        foreach (String api in KeyApis)
            features[offset++] = apiSet.Contains(api) ? 1 : 0;

        return offset;
    }

    private Int32 ExtractExportFeatures(FeatureVector features, Int32 offset)
    {
        ExportFunction[]? exports = _peFile?.ExportedFunctions;
        ImageExportDirectory? exportDir = _peFile?.ImageExportDirectory;

        features[offset++] = exportDir is not null ? 1 : 0;
        features[offset++] = exports?.Length ?? 0;

        Int32 namedExports = exports?.Count(e => !String.IsNullOrEmpty(e.Name)) ?? 0;
        features[offset++] = namedExports;

        String[] suspiciousExports = ["ServiceMain", "DllMain", "DriverEntry"];
        Boolean hasSuspicious = exports?.Any(e => suspiciousExports.Contains(e.Name, StringComparer.OrdinalIgnoreCase)) ?? false;
        features[offset++] = hasSuspicious ? 1 : 0;

        String[] exportNames = exports?.Where(e => !String.IsNullOrEmpty(e.Name)).Select(e => e.Name!).ToArray() ?? [];
        features[offset++] = exportNames.Length > 0 ? EntropyCalculator.CalculateFromString(String.Join("", exportNames)) : 0;

        return offset;
    }

    private Int32 ExtractResourceFeatures(FeatureVector features, Int32 offset)
    {
        ImageResourceDirectory? resourceDir = _peFile?.ImageResourceDirectory;

        List<ImageResourceDirectoryEntry?>? dirEntries = resourceDir?.DirectoryEntries;
        features[offset++] = dirEntries?.Count ?? 0;

        Int32 typeCount = dirEntries?.Select(r => r?.ID.ToString() ?? "").Distinct().Count() ?? 0;
        features[offset++] = typeCount;

        List<Double> resourceEntropies = [];
        if (dirEntries is not null)
        {
            foreach (ImageResourceDirectoryEntry? entry in dirEntries)
            {
                if (entry is null) continue;
                Byte[] resBytes = GetResourceEntryBytes(entry);
                if (resBytes.Length > 0)
                    resourceEntropies.Add(EntropyCalculator.Calculate(resBytes));
            }
        }

        features[offset++] = resourceEntropies.Count > 0 ? resourceEntropies.Average() : 0;
        features[offset++] = resourceEntropies.Count > 0 ? resourceEntropies.Max() : 0;

        Boolean hasPeResource = dirEntries?.Any(e => e is not null && HasPeResourceInEntry(e)) ?? false;
        features[offset++] = hasPeResource ? 1 : 0;

        return offset;
    }

    private Int32 ExtractStringFeatures(FeatureVector features, Int32 offset)
    {
        List<String> strings = ExtractStrings(_fileBytes);

        features[offset++] = strings.Count;
        features[offset++] = strings.Count > 0 ? strings.Average(s => s.Length) : 0;
        features[offset++] = strings.Count > 0 ? EntropyCalculator.CalculateFromString(String.Join("", strings)) : 0;

        String allStrings = String.Join(" ", strings);
        Regex urlPattern = new(@"https?://", RegexOptions.IgnoreCase);
        Regex ipPattern = new(@"\b(?:\d{1,3}\.){3}\d{1,3}\b");

        features[offset++] = urlPattern.Matches(allStrings).Count;
        features[offset++] = ipPattern.Matches(allStrings).Count;

        foreach (String substr in KeySubstrings)
        {
            features[offset++] = allStrings.Contains(substr, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        return offset;
    }

    private Int32 ExtractEntryPointFeatures(FeatureVector features, Int32 offset)
    {
        ImageOptionalHeader? optHeader = _peFile?.ImageNtHeaders?.OptionalHeader;
        ImageSectionHeader[]? sections = _peFile?.ImageSectionHeaders;

        if (optHeader is null || sections is null)
        {
            for (Int32 i = 0; i < 10; i++)
                features[offset + i] = 0;
            return offset + 10;
        }

        UInt32 epRva = optHeader.AddressOfEntryPoint;
        ImageSectionHeader? epSection = FindSectionByRva(epRva, sections);

        features[offset++] = epSection is not null ? Array.IndexOf(sections, epSection) : 20;
        features[offset++] = epSection is not null ? GetSectionEntropy(epSection) : 0;
        features[offset++] = epSection is not null ? epRva - epSection.VirtualAddress : 0;

        Byte[] epBytes = GetEpBytes(epRva, 64);
        features[offset++] = EntropyCalculator.Calculate(epBytes);

        Byte[] ep16Bytes = GetEpBytes(epRva, 16);
        (Int32 PushCount, Int32 CallCount, Int32 JmpCount) instructionCounts = CountInstructions(ep16Bytes);
        features[offset++] = instructionCounts.PushCount;
        features[offset++] = instructionCounts.CallCount;
        features[offset++] = instructionCounts.JmpCount;

        features[offset++] = epSection is not null && epSection.Characteristics.HasFlag(ScnCharacteristicsType.MemExecute) ? 1 : 0;
        features[offset++] = epSection is not null && epSection.Name == ".text" && (epRva - epSection.VirtualAddress) <= 0x1000 ? 1 : 0;

        Boolean hasLegacyProlog = ep16Bytes.Length >= 4 && ep16Bytes[0] == 0x55 && ep16Bytes[1] == 0x8B && ep16Bytes[2] == 0xEC;
        features[offset++] = hasLegacyProlog ? 1 : 0;

        return offset;
    }

    private Int32 ExtractRelocationTlsFeatures(FeatureVector features, Int32 offset)
    {
        ImageBaseRelocation[]? relocDir = _peFile?.ImageRelocationDirectory;
        ImageTlsDirectory? tlsDir = _peFile?.ImageTlsDirectory;
        RuntimeFunction[]? exceptionDir = _peFile?.ExceptionDirectory;
        ImportFunction[]? imports = _peFile?.ImportedFunctions;

        features[offset++] = relocDir is not null && relocDir.Length > 0 ? 1 : 0;

        Int32 relocSize = 0;
        if (relocDir is not null)
        {
            foreach (ImageBaseRelocation reloc in relocDir)
                relocSize += (Int32)reloc.SizeOfBlock;
        }
        features[offset++] = relocSize;

        Int32 relocCount = 0;
        if (relocDir is not null)
        {
            foreach (ImageBaseRelocation reloc in relocDir)
                relocCount += (Int32)((reloc.SizeOfBlock - 8) / 2);
        }
        features[offset++] = relocCount;

        features[offset++] = tlsDir is not null ? 1 : 0;

        Int32 tlsCallbackCount = 0;
        if (tlsDir?.TlsCallbacks is not null && tlsDir.TlsCallbacks.Length > 0)
        {
            tlsCallbackCount = tlsDir.TlsCallbacks.Length;
        }
        features[offset++] = tlsCallbackCount;

        features[offset++] = exceptionDir is not null && exceptionDir.Length > 0 ? 1 : 0;

        HashSet<String> importSet = imports is not null
            ? new HashSet<String>(imports.Select(i => i.Name ?? ""), StringComparer.OrdinalIgnoreCase)
            : [];

        features[offset++] = importSet.Contains("VirtualAllocEx") && importSet.Contains("WriteProcessMemory") ? 1 : 0;
        features[offset++] = importSet.Contains("socket") && importSet.Contains("connect") ? 1 : 0;

        ImageOptionalHeader? optHeader = _peFile?.ImageNtHeaders?.OptionalHeader;
        features[offset++] = optHeader is not null && optHeader.SectionAlignment != optHeader.FileAlignment ? 1 : 0;

        ImageSectionHeader[]? sections = _peFile?.ImageSectionHeaders;
        Boolean hasMismatch = sections?.Any(s => s.SizeOfRawData == 0 && s.VirtualSize > 0) ?? false;
        features[offset++] = hasMismatch ? 1 : 0;

        return offset;
    }

    private Int32 ExtractByteStatistics(FeatureVector features, Int32 offset)
    {
        if (_fileBytes.Length == 0)
        {
            for (Int32 i = 0; i < 20; i++)
                features[offset + i] = 0;
            return offset + 20;
        }

        Int32[] freq = new Int32[256];
        foreach (Byte b in _fileBytes)
            freq[b]++;

        Double mean = _fileBytes.Average(b => (Double)b);
        features[offset++] = mean;

        Double variance = _fileBytes.Average(b => Math.Pow(b - mean, 2));
        features[offset++] = variance;

        Double stdDev = Math.Sqrt(variance);
        Double skewness = _fileBytes.Average(b => Math.Pow((b - mean) / (stdDev + 1e-10), 3));
        features[offset++] = skewness;

        Double kurtosis = _fileBytes.Average(b => Math.Pow((b - mean) / (stdDev + 1e-10), 4)) - 3;
        features[offset++] = kurtosis;

        Byte[] sortedBytes = _fileBytes.OrderBy(b => b).ToArray();
        features[offset++] = sortedBytes[_fileBytes.Length / 2];

        Double mode = (Double)freq.Select((f, i) => (f, i)).OrderByDescending(x => x.f).First().i;
        features[offset++] = mode;

        Double[] top10Freq = freq.Select((f, i) => (f, i))
            .OrderByDescending(x => x.f)
            .Take(10)
            .Select(x => x.f / (Double)_fileBytes.Length)
            .ToArray();

        for (Int32 i = 0; i < 10; i++)
            features[offset++] = i < top10Freq.Length ? top10Freq[i] : 0;

        Double autocorr = CalculateAutocorrelation(_fileBytes, 1);
        features[offset++] = autocorr;

        return offset;
    }

    private Int32 ExtractSupplementaryFeatures(FeatureVector features, Int32 offset)
    {
        ImportFunction[]? imports = _peFile?.ImportedFunctions;
        HashSet<String> importSet = imports is not null
            ? new HashSet<String>(imports.Select(i => i.Name ?? ""), StringComparer.OrdinalIgnoreCase)
            : [];

        features[offset++] = CountApiCategory(importSet, ["CreateProcess", "ShellExecute", "WinExec"]);
        features[offset++] = CountApiCategory(importSet, ["RegCreateKeyEx", "CreateService"]);
        features[offset++] = CountApiCategory(importSet, ["OpenProcessToken", "AdjustTokenPrivileges"]);
        features[offset++] = CountApiCategory(importSet, ["VirtualProtect", "IsDebuggerPresent"]);
        features[offset++] = CountApiCategory(importSet, ["CredRead", "CryptUnprotectData"]);
        features[offset++] = CountApiCategory(importSet, ["GetComputerName", "NetServerEnum"]);
        features[offset++] = CountApiCategory(importSet, ["NetUseAdd", "CreateRemoteThread"]);
        features[offset++] = CountApiCategory(importSet, ["GetAsyncKeyState", "SetWindowsHookEx"]);
        features[offset++] = CountApiCategory(importSet, ["URLDownloadToFile", "InternetOpen"]);
        features[offset++] = CountApiCategory(importSet, ["send", "FtpPutFile"]);

        ImageSectionHeader[] sections = _peFile?.ImageSectionHeaders ?? [];
        foreach (String suspName in SuspiciousSectionNames)
        {
            features[offset++] = sections.Any(s => s.Name.Equals(suspName, StringComparison.OrdinalIgnoreCase)) ? 1 : 0;
        }

        ImageOptionalHeader? optHeader = _peFile?.ImageNtHeaders?.OptionalHeader;
        UInt32 epRva = optHeader?.AddressOfEntryPoint ?? 0;
        Byte[] epBytes = GetEpBytes(epRva, 256);

        Double[] ngramFeatures = ExtractInstructionNgrams(epBytes);
        foreach (Double ngram in ngramFeatures)
            features[offset++] = ngram;

        Double[] tailFeatures = ExtractTailFeatures();
        foreach (Double tail in tailFeatures)
            features[offset++] = tail;

        return offset;
    }

    private static Double CalculateCompressionRatio(Byte[] data)
    {
        if (data.Length == 0) return 0;

        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }

        return output.Length / (Double)data.Length;
    }

    private Double GetSectionEntropy(ImageSectionHeader section)
    {
        if (section.PointerToRawData >= _fileBytes.Length || section.SizeOfRawData == 0)
            return 0;

        UInt32 end = Math.Min(section.PointerToRawData + section.SizeOfRawData, (UInt32)_fileBytes.Length);
        Int32 length = (Int32)(end - section.PointerToRawData);
        ReadOnlySpan<Byte> sectionBytes = new(_fileBytes, (Int32)section.PointerToRawData, length);

        return EntropyCalculator.Calculate(sectionBytes);
    }

    private static Boolean IsStandardSectionName(String name)
    {
        String[] standardNames = [".text", ".data", ".rdata", ".rsrc", ".reloc"];
        return standardNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    private Double CalculateStringDensity(ImageSectionHeader section)
    {
        if (section.PointerToRawData >= _fileBytes.Length || section.SizeOfRawData == 0)
            return 0;

        UInt32 end = Math.Min(section.PointerToRawData + section.SizeOfRawData, (UInt32)_fileBytes.Length);
        Int32 length = (Int32)(end - section.PointerToRawData);
        Int32 stringCount = 0;
        Int32 currentLen = 0;

        for (Int32 i = (Int32)section.PointerToRawData; i < end; i++)
        {
            Byte b = _fileBytes[i];
            if (b >= 32 && b < 127)
            {
                currentLen++;
            }
            else
            {
                if (currentLen >= 4)
                    stringCount += currentLen;
                currentLen = 0;
            }
        }

        if (currentLen >= 4)
            stringCount += currentLen;

        return length > 0 ? stringCount / (Double)length : 0;
    }

    private static ImageSectionHeader? FindSectionByRva(UInt32 rva, ImageSectionHeader[] sections)
    {
        foreach (ImageSectionHeader sec in sections)
        {
            if (rva >= sec.VirtualAddress && rva < sec.VirtualAddress + sec.VirtualSize)
                return sec;
        }
        return null;
    }

    private Byte[] GetEpBytes(UInt32 epRva, Int32 count)
    {
        ImageSectionHeader[]? sections = _peFile?.ImageSectionHeaders;
        if (sections is null) return [];

        ImageSectionHeader? section = FindSectionByRva(epRva, sections);
        if (section is null) return [];

        UInt32 offset = section.PointerToRawData + (epRva - section.VirtualAddress);
        if (offset >= _fileBytes.Length) return [];

        Int32 available = Math.Min(count, _fileBytes.Length - (Int32)offset);
        Byte[] result = new Byte[available];
        Array.Copy(_fileBytes, (Int32)offset, result, 0, available);
        return result;
    }

    private (Int32 PushCount, Int32 CallCount, Int32 JmpCount) CountInstructions(Byte[] code)
    {
        if (code.Length < 2) return (0, 0, 0);

        Int32 pushCount = 0, callCount = 0, jmpCount = 0;

        try
        {
            using CapstoneX86Disassembler disassembler =
                CapstoneDisassembler.CreateX86Disassembler(
                    _peFile?.Is64Bit == true ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32);
            disassembler.EnableInstructionDetails = false;
            disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

            X86Instruction[] instructions = disassembler.Disassemble(code);
            foreach (X86Instruction insn in instructions)
            {
                String mnemonic = insn.Mnemonic.ToString().ToLowerInvariant();
                if (mnemonic == "push") pushCount++;
                else if (mnemonic == "call") callCount++;
                else if (mnemonic.StartsWith('j')) jmpCount++;
            }
        }
        catch
        {
            pushCount = code.Count(b => b == 0x68 || b == 0x6A);
            callCount = code.Count(b => b == 0xE8 || b == 0xFF);
            jmpCount = code.Count(b => b is >= 0x70 and <= 0x7F || b == 0xE9 || b == 0xEB);
        }

        return (pushCount, callCount, jmpCount);
    }

    private Byte[] GetResourceEntryBytes(ImageResourceDirectoryEntry entry)
    {
        try
        {
            if (entry is null) return [];

            ImageResourceDataEntry? dataEntry = entry.ResourceDataEntry;
            if (dataEntry is null) return [];

            UInt32 dataRva = dataEntry.OffsetToData;
            UInt32 size = dataEntry.Size1;

            if (dataRva > 0 && size > 0 && dataRva + size <= _fileBytes.Length)
            {
                Byte[] result = new Byte[size];
                Array.Copy(_fileBytes, (Int32)dataRva, result, 0, (Int32)size);
                return result;
            }
        }
        catch { }
        return [];
    }

    private Boolean HasPeResourceInEntry(ImageResourceDirectoryEntry entry)
    {
        try
        {
            Byte[] data = GetResourceEntryBytes(entry);
            return data.Length >= 2 && data[0] == 0x4D && data[1] == 0x5A;
        }
        catch { }
        return false;
    }

    private static List<String> ExtractStrings(Byte[] data, Int32 minLength = 4)
    {
        List<String> strings = [];
        StringBuilder current = new();

        foreach (Byte b in data)
        {
            if (b >= 32 && b < 127)
            {
                current.Append((Char)b);
            }
            else
            {
                if (current.Length >= minLength)
                    strings.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length >= minLength)
            strings.Add(current.ToString());

        return strings;
    }

    private static Double CalculateAutocorrelation(Byte[] data, Int32 lag)
    {
        if (data.Length <= lag) return 0;

        Double mean = data.Average(b => (Double)b);
        Double variance = data.Average(b => Math.Pow(b - mean, 2));

        if (variance == 0) return 0;

        Double sum = 0;
        for (Int32 i = 0; i < data.Length - lag; i++)
        {
            sum += (data[i] - mean) * (data[i + lag] - mean);
        }

        return sum / ((data.Length - lag) * variance);
    }

    private static Int32 CountApiCategory(HashSet<String> importSet, String[] apis)
    {
        return apis.Count(api => importSet.Contains(api));
    }

    private Double[] ExtractInstructionNgrams(Byte[] code)
    {
        Double[] result = new Double[15];
        if (code.Length < 16) return result;

        try
        {
            using CapstoneX86Disassembler disassembler =
                CapstoneDisassembler.CreateX86Disassembler(
                    _peFile?.Is64Bit == true ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32);
            disassembler.EnableInstructionDetails = false;
            disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

            X86Instruction[] instructions = disassembler.Disassemble(code);
            List<X86Instruction> insnList = [.. instructions.Take(64)];

            (Int64, Int64)[] ranges = [(0, 64), (64, 128), (128, 256)];

            for (Int32 r = 0; r < 3; r++)
            {
                (Int64 start, Int64 end) = ranges[r];
                List<X86Instruction> rangeInsns = insnList.Where(i => (Int64)i.Address >= start && (Int64)i.Address < end).ToList();
                Int32 total = Math.Max(rangeInsns.Count, 1);

                Int32 pushPop = 0, movImm = 0, callInd = 0, intBrk = 0, ret = 0;

                foreach (X86Instruction insn in rangeInsns)
                {
                    String mnemonicStr = insn.Mnemonic.ToString();
                    if (mnemonicStr is "push" or "pop") pushPop++;
                    if (mnemonicStr == "mov" && insn.Bytes.Length > 2) movImm++;
                    if (mnemonicStr == "call") callInd++;
                    if (mnemonicStr is "int3" or "int" or "int1") intBrk++;
                    if (mnemonicStr is "ret" or "retn") ret++;
                }

                result[r * 5 + 0] = pushPop / (Double)total;
                result[r * 5 + 1] = movImm / (Double)total;
                result[r * 5 + 2] = callInd / (Double)total;
                result[r * 5 + 3] = intBrk / (Double)total;
                result[r * 5 + 4] = ret / (Double)total;
            }
        }
        catch { }

        return result;
    }

    private Double[] ExtractTailFeatures()
    {
        Double[] result = new Double[10];
        if (_fileBytes.Length == 0) return result;

        Int32 tailStart = Math.Max(0, _fileBytes.Length - 1024);
        Int32 tailLength = _fileBytes.Length - tailStart;
        ReadOnlySpan<Byte> tailBytes = new(_fileBytes, tailStart, tailLength);

        result[0] = EntropyCalculator.Calculate(tailBytes);

        ImageSectionHeader[]? sections = _peFile?.ImageSectionHeaders;
        UInt32 totalRawSize = (UInt32)(sections?.Sum(s => s.SizeOfRawData) ?? 0);
        Boolean hasOverlay = totalRawSize < _fileBytes.Length;
        result[1] = hasOverlay ? 1 : 0;

        Int64 overlaySize = _fileBytes.Length - totalRawSize;
        result[2] = Math.Max(0, overlaySize);

        if (hasOverlay && overlaySize > 0 && totalRawSize < _fileBytes.Length)
        {
            ReadOnlySpan<Byte> overlaySpan = new(_fileBytes, (Int32)totalRawSize, (Int32)overlaySize);
            result[3] = EntropyCalculator.Calculate(overlaySpan);
        }

        result[4] = tailBytes.Length >= 2 && tailBytes[0] == 0x4D && tailBytes[1] == 0x5A ? 1 : 0;
        if (tailBytes.Length >= 2 && tailBytes[0] == 0x50 && tailBytes[1] == 0x4B)
            result[4] = 1;

        Int32 last256 = Math.Min(256, tailBytes.Length);
        Int32 zeroCount = 0;
        for (Int32 i = tailBytes.Length - last256; i < tailBytes.Length; i++)
        {
            if (tailBytes[i] == 0) zeroCount++;
        }
        result[5] = last256 > 0 ? zeroCount / (Double)last256 : 0;

        Int32 tail256Start = Math.Max(0, tailBytes.Length - 256);
        ReadOnlySpan<Byte> tail256Span = tailBytes[tail256Start..];
        if (tail256Span.Length > 0)
        {
            Double sum = 0;
            foreach (Byte b in tail256Span) sum += b;
            result[6] = sum / tail256Span.Length;

            Double varSum = 0;
            Double mean = result[6];
            foreach (Byte b in tail256Span) varSum += Math.Pow(b - mean, 2);
            result[7] = varSum / tail256Span.Length;
        }

        for (Int32 i = 0; i < tailBytes.Length - 1; i++)
        {
            if (tailBytes[i] == 0x4D && tailBytes[i + 1] == 0x5A)
            {
                result[8] = 1;
                break;
            }
        }

        ImageDataDirectory[]? dataDirs = _peFile?.ImageNtHeaders?.OptionalHeader?.DataDirectory;
        if (dataDirs is not null && dataDirs.Length > 4)
        {
            ImageDataDirectory certDir = dataDirs[4];
            result[9] = certDir.VirtualAddress > 0 ? _fileBytes.Length - (Int32)certDir.VirtualAddress : 0;
        }

        return result;
    }
}
