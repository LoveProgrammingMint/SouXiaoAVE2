// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;

using SouXiaoAVE.DataExtraction;
using SouXiaoAVE.DataProcessing;
using SouXiaoAVE.Interfaces;
using SouXiaoAVE.Utils;

namespace SouXiaoAVE;

internal class Entry
{
    static void Main()
    {
        String notepadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");

        if (!File.Exists(notepadPath))
        {
            Console.WriteLine($"Notepad not found at: {notepadPath}");
            return;
        }

        Console.WriteLine($"Processing: {notepadPath}");
        Console.WriteLine();

        Console.WriteLine("=== ReadRawBytes ===");
        using (ReadRawBytes rawBytesExtractor = new())
        {
            List<Single> rawBytes = rawBytesExtractor.Extract(notepadPath);
            Console.WriteLine($"Extracted {rawBytes.Count} raw bytes");
            Console.WriteLine($"First 10 values: {String.Join(", ", rawBytes.GetRange(0, Math.Min(10, rawBytes.Count)))}");
        }
        Console.WriteLine();

        Console.WriteLine("=== CalculateEntropy ===");
        using (CalculateEntropy entropyExtractor = new())
        {
            List<Single> entropyValues = entropyExtractor.Extract(notepadPath);
            Console.WriteLine($"Extracted {entropyValues.Count} entropy segments");
            Console.WriteLine($"First 10 values: {String.Join(", ", entropyValues.GetRange(0, Math.Min(10, entropyValues.Count)))}");
        }
        Console.WriteLine();

        Console.WriteLine("=== StatisticalInformations ===");
        using (StatisticalInformations statsExtractor = new())
        {
            List<Single> stats = statsExtractor.Extract(notepadPath);
            Console.WriteLine($"Extracted {stats.Count} statistical features");
            Console.WriteLine($"First 10 values: {String.Join(", ", stats.GetRange(0, Math.Min(10, stats.Count)))}");
        }
        Console.WriteLine();

        Console.WriteLine("=== AssemblyArray ===");
        try
        {
            using (AssemblyArray assemblyExtractor = new())
            {
                List<Single> assemblyTokens = assemblyExtractor.Extract(notepadPath);
                Console.WriteLine($"Extracted {assemblyTokens.Count} assembly tokens");
                Console.WriteLine($"First 10 values: {String.Join(", ", assemblyTokens.GetRange(0, Math.Min(10, assemblyTokens.Count)))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AssemblyArray extraction failed: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("=== DataExtractor with Preprocessor ===");
        using (DataExtractor extractor = new())
        using (DataPreprocessor preprocessor = new())
        {
            extractor.SetExtractor(new StatisticalInformations());
            extractor.SetPreprocessor(preprocessor);

            DataExtractionResult result = extractor.Extract(notepadPath);
            Console.WriteLine($"Extractor: {result.ExtractorName}");
            Console.WriteLine($"Feature Count: {result.FeatureCount}");
            Console.WriteLine($"Is Valid: {result.IsValid}");
            Console.WriteLine($"Extraction Time: {result.ExtractionTime}");
        }
        Console.WriteLine();

        Console.WriteLine("=== Async Extraction ===");
        using (ReadRawBytes asyncExtractor = new())
        {
            List<Single> asyncResult = asyncExtractor.ExtractAsync(notepadPath).Result;
            Console.WriteLine($"Async extracted {asyncResult.Count} features");
        }

        Console.WriteLine();
        Console.WriteLine("All extractions completed successfully!");
    }
}
