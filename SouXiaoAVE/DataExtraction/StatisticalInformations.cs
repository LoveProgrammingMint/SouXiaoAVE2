// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PeNet;
using PeNet.Header.Pe;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataExtraction;

internal class StatisticalInformations : IDataExtraction
{
    public String ExtractorName { get; set; } = "Statistical Features";
    public IDataPreprocessor? Preprocessor { get; set; }

    public List<Single> Extract(String filePath)
    {
        List<Single> features = [];
        PeFile pe = new(filePath);
        Byte[] rawFile = File.ReadAllBytes(filePath);

        features.Add((Single)rawFile.Length);
        features.Add((Single)Math.Log(rawFile.Length + 1, 2));
        features.Add(pe.ImageNtHeaders?.OptionalHeader.CheckSum ?? 0);
        features.Add(pe.IsDll ? 1 : 0);
        features.Add(pe.Is64Bit ? 1 : 0);
        features.Add(pe.ImageNtHeaders?.FileHeader.TimeDateStamp ?? 0);
        features.Add(pe.ImageNtHeaders != null ? new DateTime(1970, 1, 1).AddSeconds(pe.ImageNtHeaders.FileHeader.TimeDateStamp).Year : 0);
        features.Add(PeFile.IsPeFile(filePath) ? 1 : 0);
        features.Add((Single)(pe.ImageDosHeader?.E_lfanew ?? 0));
        features.Add((Single)((pe.ImageDosHeader?.E_lfanew ?? 0) - 0x40));

        UInt32 machine = (UInt32)(pe.ImageNtHeaders?.FileHeader.Machine ?? 0);
        features.Add(machine == 0x14c ? 0 : machine == 0x8664 ? 1 : machine == 0x1c0 || machine == 0xaa64 ? 2 : 3);
        features.Add((Single)(pe.ImageNtHeaders?.FileHeader.NumberOfSections ?? 0));
        UInt16 magic = (UInt16)(pe.ImageNtHeaders?.OptionalHeader.Magic ?? 0);
        features.Add(magic == 0x10b ? 0 : magic == 0x20b ? 1 : 2);
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.AddressOfEntryPoint ?? 0));
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.BaseOfCode ?? 0));
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.SizeOfImage ?? 0));
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.SizeOfHeaders ?? 0));
        UInt32 subsystem = (UInt32)(pe.ImageNtHeaders?.OptionalHeader.Subsystem ?? 0);
        features.Add(subsystem == 2 ? 0 : subsystem == 3 ? 1 : subsystem == 7 ? 2 : 3);
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.DllCharacteristics ?? 0));
        features.Add((Single)Math.Log((pe.ImageNtHeaders?.OptionalHeader.SizeOfStackCommit ?? 1) + 1, 2));

        ImageSectionHeader[]? sections = pe.ImageSectionHeaders;
        sections ??= [];
        features.Add((Single)sections.Length);
        features.Add(CalculateStringEntropy(String.Join("", sections.Select(s => GetSectionName(s).TrimEnd('\0')))));
        features.Add(sections.Length > 0 ? sections.Average(s => (Single)s.VirtualSize) : 0);
        features.Add(sections.Length > 0 ? CalculateVariance(sections.Select(s => (Single)s.VirtualSize)) : 0);
        features.Add(sections.Length > 0 ? (Single)sections.Max(s => s.VirtualSize) : 0);
        features.Add(sections.Length > 0 ? sections.Average(s => (Single)s.SizeOfRawData) : 0);
        features.Add(sections.Length > 0 ? CalculateVariance(sections.Select(s => (Single)s.SizeOfRawData)) : 0);
        features.Add(sections.Length > 0 ? sections.Average(s => (Single)s.VirtualAddress) : 0);
        features.Add(sections.Length > 0 ? CalculateVariance(sections.Select(s => (Single)s.VirtualAddress)) : 0);
        features.Add((Single)sections.Count(s => ((UInt32)s.Characteristics & 0x20000000) != 0));
        features.Add((Single)sections.Count(s => ((UInt32)s.Characteristics & 0x80000000) != 0));
        features.Add((Single)sections.Count(s => ((UInt32)s.Characteristics & 0x40000000) != 0));
        features.Add((Single)sections.Count(s => ((UInt32)s.Characteristics & 0xA0000000) == 0xA0000000));

        List<Single> sectionEntropies = [.. sections.Select(s => CalculateSectionEntropy(rawFile, s.PointerToRawData, s.SizeOfRawData))];
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Average() : 0);
        features.Add(sectionEntropies.Count > 0 ? CalculateVariance(sectionEntropies.Select(x => x)) : 0);
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Max() : 0);
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Min() : 0);
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.FileAlignment ?? 0));
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.SectionAlignment ?? 0));
        String[] specialNames = [".rsrc", ".text", ".data", ".rdata", ".reloc", ".pdata"];
        features.Add((Single)specialNames.Count(sn => sections.Any(s => GetSectionName(s).TrimEnd('\0').Equals(sn, StringComparison.OrdinalIgnoreCase))));

        ImportFunction[]? imports = pe.ImportedFunctions;
        imports ??= [];
        List<String> importDlls = [.. imports.Select(i => i.DLL).Distinct()];
        features.Add((Single)importDlls.Count);
        features.Add((Single)imports.Length);
        features.Add(importDlls.Count > 0 ? (Single)importDlls.Average(d => imports.Count(i => i.DLL == d)) : 0);
        features.Add(importDlls.Count > 0 ? CalculateVariance(importDlls.Select(d => (Single)imports.Count(i => i.DLL == d))) : 0);
        features.Add(importDlls.Count > 0 ? (Single)importDlls.Max(d => imports.Count(i => i.DLL == d)) : 0);
        features.Add((Single)imports.Sum(i => i.Name?.Length ?? 0));
        features.Add(CalculateStringEntropy(String.Join("", imports.Select(i => i.Name ?? ""))));
        features.Add(importDlls.Any(d => d?.ToLower().Contains("kernel32", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("user32", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("advapi32", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("ntdll", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("ws2_32", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("wininet", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        features.Add(importDlls.Any(d => d?.ToLower().Contains("shell32", StringComparison.CurrentCultureIgnoreCase) ?? false) ? 1 : 0);
        String[] sensitiveApis = ["CreateRemoteThread", "WriteProcessMemory", "VirtualAllocEx", "ReadProcessMemory", "OpenProcess", "LoadLibrary", "GetProcAddress", "VirtualProtect", "NtUnmapViewOfSection", "NtCreateThreadEx"];
        Int32 sensitiveCount = imports.Count(i => sensitiveApis.Contains(i.Name));
        features.Add((Single)sensitiveCount);
        features.Add(imports.Length > 0 ? (Single)sensitiveCount / imports.Length : 0);

        ImageDataDirectory? importDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[1];
        features.Add((Single)(importDir?.Size ?? 0));
        features.Add((Single)(importDir?.VirtualAddress ?? 0));
        ImageDataDirectory? delayImportDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[13];
        features.Add(delayImportDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add(importDir?.Size > 0 && pe.ImageNtHeaders != null ? (Single)(importDir.VirtualAddress - pe.ImageNtHeaders.OptionalHeader.ImageBase) : 0);

        ExportFunction[]? exports = pe.ExportedFunctions;
        exports ??= [];
        features.Add(exports.Length > 0 ? 1 : 0);
        features.Add((Single)exports.Length);
        features.Add(CalculateStringEntropy(String.Join("", exports.Select(e => e.Name ?? ""))));
        features.Add(exports.Length > 0 ? (Single)exports.Average(e => e.Address) : 0);
        features.Add(exports.Length > 0 ? CalculateVariance(exports.Select(e => (Single)e.Address)) : 0);

        ImageSectionHeader? rsrcSection = sections.FirstOrDefault(s => GetSectionName(s).TrimEnd('\0').Equals(".rsrc", StringComparison.OrdinalIgnoreCase));
        features.Add(rsrcSection != null ? 1 : 0);

        PeNet.Header.Resource.Resources? resources = pe.Resources;
        Int32 rsrcTypeCount = 0;
        Int64 rsrcTotalSize = 0;
        List<Int64> rsrcSizes = [];
        if (resources != null)
        {
            try
            {
                ImageDataDirectory? rsrcDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[2];
                if (rsrcDir != null && rsrcDir.VirtualAddress != 0)
                {
                    rsrcTypeCount = 1;
                    rsrcTotalSize = rsrcDir.Size;
                    rsrcSizes.Add(rsrcDir.Size);
                }
            }
            catch
            {
            }
        }
        features.Add((Single)rsrcTypeCount);
        features.Add((Single)rsrcTotalSize);
        features.Add(rsrcSizes.Count > 0 ? (Single)rsrcSizes.Average() : 0);
        features.Add(rsrcSizes.Count > 0 ? CalculateVariance(rsrcSizes.Select(s => (Single)s)) : 0);
        String rsrcNames = "";
        features.Add(CalculateStringEntropy(rsrcNames));
        Boolean hasVersion = rsrcSection != null;
        features.Add(hasVersion ? 1 : 0);
        Boolean hasManifest = false;
        features.Add(hasManifest ? 1 : 0);
        Boolean hasIcon = false;
        features.Add(hasIcon ? 1 : 0);
        Boolean hasCursor = false;
        features.Add(hasCursor ? 1 : 0);

        List<String> strings = ExtractStrings(rawFile);
        features.Add((Single)strings.Count);
        features.Add((Single)strings.Sum(s => s.Length));
        features.Add(strings.Count > 0 ? (Single)strings.Average(s => s.Length) : 0);
        features.Add(strings.Count > 0 ? CalculateVariance(strings.Select(s => (Single)s.Length)) : 0);
        features.Add(strings.Count > 0 ? (Single)strings.Max(s => s.Length) : 0);
        features.Add(strings.Count > 0 ? (Single)strings.Min(s => s.Length) : 0);
        String allStrings = String.Join("", strings);
        features.Add(allStrings.Length > 0 ? (Single)allStrings.Count(char.IsDigit) / allStrings.Length : 0);
        features.Add(allStrings.Length > 0 ? (Single)allStrings.Count(char.IsLetter) / allStrings.Length : 0);
        features.Add(allStrings.Length > 0 ? (Single)allStrings.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) / allStrings.Length : 0);
        features.Add(allStrings.Length > 0 ? (Single)allStrings.Count(char.IsWhiteSpace) / allStrings.Length : 0);
        features.Add((Single)strings.Count(s => s.Contains("http://")));
        features.Add((Single)strings.Count(s => s.Contains("C:\\")));
        features.Add((Single)strings.Count(s => s.Contains("cmd.exe")));
        features.Add((Single)strings.Count(s => s.Contains("powershell", StringComparison.CurrentCultureIgnoreCase)));
        features.Add((Single)strings.Count(s => s.Contains("CreateRemoteThread")));

        features.Add(CalculateByteEntropy(rawFile));
        features.Add(rawFile.Length >= 512 ? CalculateByteEntropy([.. rawFile.Take(512)]) : CalculateByteEntropy(rawFile));
        Int32 dosStubSize = (Int32)((pe.ImageDosHeader?.E_lfanew ?? 0) - 0x40);
        features.Add(dosStubSize > 0 && rawFile.Length > 0x40 + dosStubSize ? CalculateByteEntropy([.. rawFile.Skip(0x40).Take(dosStubSize)]) : 0);
        ImageSectionHeader? textSection = sections.FirstOrDefault(s => GetSectionName(s).TrimEnd('\0').Equals(".text", StringComparison.OrdinalIgnoreCase));
        features.Add(textSection != null ? CalculateSectionEntropy(rawFile, textSection.PointerToRawData, textSection.SizeOfRawData) : 0);
        ImageSectionHeader? rdataSection = sections.FirstOrDefault(s => GetSectionName(s).TrimEnd('\0').Equals(".rdata", StringComparison.OrdinalIgnoreCase));
        features.Add(rdataSection != null ? CalculateSectionEntropy(rawFile, rdataSection.PointerToRawData, rdataSection.SizeOfRawData) : 0);
        ImageSectionHeader? dataSection = sections.FirstOrDefault(s => GetSectionName(s).TrimEnd('\0').Equals(".data", StringComparison.OrdinalIgnoreCase));
        features.Add(dataSection != null ? CalculateSectionEntropy(rawFile, dataSection.PointerToRawData, dataSection.SizeOfRawData) : 0);
        features.Add(rsrcSection != null ? CalculateSectionEntropy(rawFile, rsrcSection.PointerToRawData, rsrcSection.SizeOfRawData) : 0);
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Max() : 0);
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Min() : 0);
        features.Add(sectionEntropies.Count > 0 ? sectionEntropies.Average() : 0);

        Int32[] histogram = new Int32[256];
        foreach (Byte b in rawFile) histogram[b]++;
        Single byteMean = rawFile.Length > 0 ? histogram.Select((c, i) => c * i).Sum() / (Single)rawFile.Length : 0;
        features.Add(byteMean);
        Single byteVar = 0;
        if (rawFile.Length > 0)
        {
            byteVar = histogram.Select((c, i) => c * (i - byteMean) * (i - byteMean)).Sum() / rawFile.Length;
        }
        features.Add(byteVar);
        Single byteStdDev = (Single)Math.Sqrt(byteVar);
        features.Add(byteStdDev > 0 ? histogram.Select((c, i) => c * (Single)Math.Pow(i - byteMean, 3)).Sum() / (rawFile.Length * byteStdDev * byteStdDev * byteStdDev) : 0);
        features.Add(byteVar > 0 ? histogram.Select((c, i) => c * (Single)Math.Pow(i - byteMean, 4)).Sum() / (rawFile.Length * byteVar * byteVar) - 3 : 0);
        features.Add((Single)histogram.Max() / rawFile.Length);
        features.Add((Single)histogram.Where(c => c > 0).Min() / rawFile.Length);
        features.Add((Single)histogram[0] / rawFile.Length);
        features.Add((Single)histogram[255] / rawFile.Length);
        features.Add((Single)histogram.Skip(32).Take(95).Sum() / rawFile.Length);
        features.Add((Single)(histogram[0] + histogram[0xcc]) / rawFile.Length);

        features.Add(3);

        Single maxSectionEntropy = sectionEntropies.Count > 0 ? sectionEntropies.Max() : 0;
        Single packedScore = 0;
        if (maxSectionEntropy > 7.2) packedScore += 0.3f;
        if (sections.Any(s => GetSectionName(s).Contains("UPX") || GetSectionName(s).Contains("ASPACK"))) packedScore += 0.4f;
        if (pe.ImageImportDescriptors?.Length == 0) packedScore += 0.3f;
        features.Add(packedScore > 0.5f ? 1 : 0);
        features.Add(packedScore);

        Boolean isUpx = sections.Any(s => GetSectionName(s).TrimEnd('\0').Equals("UPX0", StringComparison.OrdinalIgnoreCase) ||
                                    GetSectionName(s).TrimEnd('\0').Equals("UPX1", StringComparison.OrdinalIgnoreCase) ||
                                    GetSectionName(s).TrimEnd('\0').Equals("UPX2", StringComparison.OrdinalIgnoreCase));
        features.Add(isUpx ? 1 : 0);

        Single stringEntropyVar = strings.Count > 0 ? CalculateVariance(strings.Select(s => (Single)s.Length)) : 0;
        Single avgStringLen = strings.Count > 0 ? (Single)strings.Average(s => s.Length) : 0;
        Boolean isObfuscated = (stringEntropyVar < 5 && strings.Count > 100) || avgStringLen < 6;
        features.Add(isObfuscated ? 1 : 0);

        String[] suspiciousSections = [".upx", ".aspack", ".petite", ".fsg", ".mew", ".nsp", ".pecompact"];
        Int32 suspiciousCount = sections.Count(s => suspiciousSections.Any(ss => GetSectionName(s).Contains(ss, StringComparison.CurrentCultureIgnoreCase)));
        features.Add((Single)suspiciousCount);

        ImageDataDirectory? securityDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[4];
        features.Add(securityDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add(securityDir?.Size > 0 ? (Single)Math.Log(securityDir.Size, 2) : 0);

        ImageDataDirectory? relocDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[5];
        features.Add(relocDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add((Single)Math.Log((relocDir?.Size ?? 1) + 1, 2));

        ImageDataDirectory? tlsDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[9];
        features.Add(tlsDir?.VirtualAddress != 0 ? 1 : 0);
        Int32 tlsCallbackCount = pe.ImageTlsDirectory?.AddressOfCallBacks != null ? 1 : 0;
        features.Add((Single)tlsCallbackCount);

        ImageDataDirectory? exceptionDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[3];
        features.Add(exceptionDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add((Single)Math.Log((exceptionDir?.Size ?? 1) + 1, 2));

        ImageDataDirectory? debugDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[6];
        features.Add(debugDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add((Single)Math.Log((debugDir?.Size ?? 1) + 1, 2));

        ImageDataDirectory? loadConfigDir = pe.ImageNtHeaders?.OptionalHeader.DataDirectory[10];
        features.Add(loadConfigDir?.VirtualAddress != 0 ? 1 : 0);
        features.Add((Single)(pe.ImageNtHeaders?.OptionalHeader.DllCharacteristics ?? 0));

        return Preprocessor?.Preprocess(features) ?? features;
    }

    private static String GetSectionName(ImageSectionHeader section) => section.Name ?? String.Empty;

    public async Task<List<Single>> ExtractAsyns(String source) => await Task.Run(() => Extract(source));

    public void Dispose() => GC.SuppressFinalize(this);

    private static Single CalculateByteEntropy(Byte[] data)
    {
        if (data.Length == 0) return 0;
        Int32[] freq = new Int32[256];
        foreach (Byte b in data) freq[b]++;
        Double entropy = 0;
        for (Int32 i = 0; i < 256; i++)
        {
            if (freq[i] > 0)
            {
                Double p = (Double)freq[i] / data.Length;
                entropy -= p * Math.Log(p, 2);
            }
        }
        return (Single)entropy;
    }

    private static Single CalculateSectionEntropy(Byte[] fileData, UInt32 offset, UInt32 size)
    {
        if (offset + size > fileData.Length) size = (UInt32)(fileData.Length - offset);
        if (size == 0) return 0;
        return CalculateByteEntropy([.. fileData.Skip((Int32)offset).Take((Int32)size)]);
    }

    private static Single CalculateStringEntropy(String str)
    {
        if (String.IsNullOrEmpty(str)) return 0;
        Char[] chars = str.ToCharArray();
        Dictionary<Char, Int32> freq = chars.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        Double entropy = 0;
        foreach (KeyValuePair<Char, Int32> kvp in freq)
        {
            Double p = (Double)kvp.Value / chars.Length;
            entropy -= p * Math.Log(p, 2);
        }
        return (Single)entropy;
    }

    private static Single CalculateVariance(IEnumerable<Single> values)
    {
        List<Single> list = [.. values];
        if (list.Count < 2) return 0;
        Single mean = list.Average();
        return list.Average(v => (v - mean) * (v - mean));
    }

    private static List<String> ExtractStrings(Byte[] data)
    {
        List<String> strings = [];
        StringBuilder current = new();
        for (Int32 i = 0; i < data.Length; i++)
        {
            Char c = (Char)data[i];
            if (c >= 32 && c <= 126)
            {
                current.Append(c);
            }
            else
            {
                if (current.Length >= 4) strings.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length >= 4) strings.Add(current.ToString());
        return strings;
    }
}
