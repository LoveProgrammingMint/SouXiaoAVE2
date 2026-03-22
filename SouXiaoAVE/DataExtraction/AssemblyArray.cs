// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Gee.External.Capstone;
using Gee.External.Capstone.X86;

using PeNet;
using PeNet.Header.Pe;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataExtraction;

internal sealed class AssemblyArray : IDataExtraction
{
    public String ExtractorName { get; set; } = "Assembly Array";
    public IDataPreprocessor? Preprocessor { get; set; }

    private const Int32 MaxTokenLength = 65536;
    private const Int32 MaxInstructions = 4096;

    private static BpeTokenizer? s_tokenizer;
    private static readonly Object s_lock = new();

    public List<Single> Extract(String filePath)
    {
        String[] disassemblyLines = DisassembleFile(filePath);
        List<Int32> tokens = TokenizeDisassembly(disassemblyLines);
        List<Single> result = ConvertToFloatList(tokens);
        return Preprocessor?.Preprocess(result) ?? result;
    }

    private static String[] DisassembleFile(String filePath)
    {
        Byte[] fileBytes = File.ReadAllBytes(filePath);
        List<String> instructions = [];

        try
        {
            PeFile pe = new(filePath);
            ImageSectionHeader[]? sections = pe.ImageSectionHeaders;
            Boolean is64Bit = pe.Is64Bit;

            if (sections != null && sections.Length > 0)
            {
                foreach (ImageSectionHeader section in sections)
                {
                    String sectionName = section.Name?.TrimEnd('\0') ?? "";
                    UInt32 characteristics = (UInt32)section.Characteristics;
                    Boolean isCodeSection = sectionName.Equals(".text", StringComparison.OrdinalIgnoreCase) ||
                                            (characteristics & 0x00000020) != 0;

                    if (isCodeSection)
                    {
                        UInt32 offset = section.PointerToRawData;
                        UInt32 size = section.SizeOfRawData;

                        if (offset + size <= fileBytes.Length && size > 0)
                        {
                            Byte[] codeBytes = new Byte[size];
                            Array.Copy(fileBytes, offset, codeBytes, 0, size);

                            String[] sectionInstructions = DisassembleCode(codeBytes, is64Bit);
                            instructions.AddRange(sectionInstructions);

                            if (instructions.Count >= MaxInstructions)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (instructions.Count == 0 && fileBytes.Length > 0)
            {
                Int32 codeSize = Math.Min(fileBytes.Length, 65536);
                Byte[] codeBytes = new Byte[codeSize];
                Array.Copy(fileBytes, 0, codeBytes, 0, codeSize);
                instructions.AddRange(DisassembleCode(codeBytes, false));
            }
        }
        catch
        {
            if (fileBytes.Length > 0)
            {
                Int32 codeSize = Math.Min(fileBytes.Length, 65536);
                Byte[] codeBytes = new Byte[codeSize];
                Array.Copy(fileBytes, 0, codeBytes, 0, codeSize);
                instructions.AddRange(DisassembleCode(codeBytes, false));
            }
        }

        return [.. instructions];
    }

    private static String[] DisassembleCode(Byte[] code, Boolean is64Bit)
    {
        List<String> instructions = [];

        try
        {
            X86DisassembleMode mode = is64Bit ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32;
            using CapstoneX86Disassembler disassembler = CapstoneDisassembler.CreateX86Disassembler(mode);
            disassembler.EnableInstructionDetails = true;
            disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

            X86Instruction[] insn = disassembler.Disassemble(code, 0x1000);

            if (insn != null)
            {
                Int32 count = Math.Min(insn.Length, MaxInstructions);
                for (Int32 i = 0; i < count; i++)
                {
                    String mnemonic = insn[i].Mnemonic ?? "";
                    String operand = insn[i].Operand ?? "";
                    String instruction = $"{mnemonic} {operand}".Trim();
                    instructions.Add(instruction);
                }
            }
        }
        catch
        {
        }

        return [.. instructions];
    }

    private static List<Int32> TokenizeDisassembly(String[] disassemblyLines)
    {
        BpeTokenizer tokenizer = GetTokenizer();
        List<Int32> allTokens = [];

        foreach (String line in disassemblyLines)
        {
            if (String.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            Int32[] tokens = tokenizer.Encode(line);
            allTokens.AddRange(tokens);

            if (allTokens.Count >= MaxTokenLength)
            {
                break;
            }
        }

        return PadOrTruncate(allTokens, MaxTokenLength);
    }

    private static BpeTokenizer GetTokenizer()
    {
        if (s_tokenizer != null)
        {
            return s_tokenizer;
        }

        lock (s_lock)
        {
            if (s_tokenizer != null)
            {
                return s_tokenizer;
            }

            String tokenizerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataProcessing", "DisasmTokenizer.json");

            if (!File.Exists(tokenizerPath))
            {
                String? basePath = AppContext.BaseDirectory;
                tokenizerPath = Path.Combine(basePath, "DataProcessing", "DisasmTokenizer.json");
            }

            if (!File.Exists(tokenizerPath))
            {
                tokenizerPath = "DataProcessing/DisasmTokenizer.json";
            }

            s_tokenizer = new BpeTokenizer(tokenizerPath);
            return s_tokenizer;
        }
    }

    private static List<Int32> PadOrTruncate(List<Int32> tokens, Int32 targetLength)
    {
        if (tokens.Count == targetLength)
        {
            return tokens;
        }

        if (tokens.Count > targetLength)
        {
            return [.. tokens.GetRange(0, targetLength)];
        }

        List<Int32> result = [.. tokens];
        Int32 padCount = targetLength - tokens.Count;
        for (Int32 i = 0; i < padCount; i++)
        {
            result.Add(0);
        }

        return result;
    }

    private static List<Single> ConvertToFloatList(List<Int32> tokens)
    {
        List<Single> result = new(tokens.Count);
        foreach (Int32 token in tokens)
        {
            result.Add((Single)token);
        }

        return result;
    }

    public async Task<List<Single>> ExtractAsync(String source) => await Task.Run(() => Extract(source));

    public void Dispose() => GC.SuppressFinalize(this);
}

internal sealed class BpeTokenizer
{
    private readonly Dictionary<String, Int32> _vocab;
    private readonly Dictionary<(String, String), Int32> _merges;
    private readonly Dictionary<Int32, String> _idToToken;
    private readonly HashSet<Int32> _specialTokenIds;

    public Int32 VocabSize => _vocab.Count;

    public BpeTokenizer(String jsonPath)
    {
        _vocab = new Dictionary<String, Int32>();
        _merges = new Dictionary<(String, String), Int32>();
        _idToToken = new Dictionary<Int32, String>();
        _specialTokenIds = [];

        LoadFromJson(jsonPath);
    }

    private void LoadFromJson(String jsonPath)
    {
        String json = File.ReadAllText(jsonPath);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("added_tokens", out JsonElement addedTokens))
        {
            foreach (JsonElement token in addedTokens.EnumerateArray())
            {
                String content = token.GetProperty("content").GetString() ?? "";
                Int32 id = token.GetProperty("id").GetInt32();
                _vocab[content] = id;
                _idToToken[id] = content;
                _specialTokenIds.Add(id);
            }
        }

        if (root.TryGetProperty("model", out JsonElement model))
        {
            if (model.TryGetProperty("vocab", out JsonElement vocab))
            {
                foreach (JsonProperty prop in vocab.EnumerateObject())
                {
                    String token = prop.Name;
                    Int32 id = prop.Value.GetInt32();

                    if (!_vocab.ContainsKey(token))
                    {
                        _vocab[token] = id;
                        _idToToken[id] = token;
                    }
                }
            }

            if (model.TryGetProperty("merges", out JsonElement merges))
            {
                Int32 mergeRank = 0;
                foreach (JsonElement merge in merges.EnumerateArray())
                {
                    String mergeStr = "";
                    if (merge.ValueKind == JsonValueKind.String)
                    {
                        mergeStr = merge.GetString() ?? "";
                    }
                    else if (merge.ValueKind == JsonValueKind.Array)
                    {
                        List<String> parts = [];
                        foreach (JsonElement elem in merge.EnumerateArray())
                        {
                            if (elem.ValueKind == JsonValueKind.String)
                            {
                                parts.Add(elem.GetString() ?? "");
                            }
                        }
                        mergeStr = String.Join(" ", parts);
                    }

                    String[] parts2 = mergeStr.Split(' ', 2);
                    if (parts2.Length == 2)
                    {
                        _merges[(parts2[0], parts2[1])] = mergeRank++;
                    }
                }
            }
        }
    }

    public Int32[] Encode(String text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return [];
        }

        text = text.ToLowerInvariant();

        List<String> tokens = Tokenize(text);
        List<Int32> ids = [];

        foreach (String token in tokens)
        {
            if (_vocab.TryGetValue(token, out Int32 id))
            {
                ids.Add(id);
            }
            else if (_vocab.TryGetValue("<unk>", out Int32 unkId))
            {
                ids.Add(unkId);
            }
            else
            {
                foreach (Char c in token)
                {
                    String charToken = c.ToString();
                    if (_vocab.TryGetValue(charToken, out Int32 charId))
                    {
                        ids.Add(charId);
                    }
                }
            }
        }

        if (_vocab.TryGetValue("<s>", out Int32 bosId))
        {
            ids.Insert(0, bosId);
        }

        if (_vocab.TryGetValue("</s>", out Int32 eosId))
        {
            ids.Add(eosId);
        }

        return [.. ids];
    }

    private List<String> Tokenize(String text)
    {
        List<String> tokens = [];
        StringBuilder currentWord = new();

        foreach (Char c in text)
        {
            if (Char.IsWhiteSpace(c))
            {
                if (currentWord.Length > 0)
                {
                    tokens.Add(currentWord.ToString());
                    currentWord.Clear();
                }
            }
            else
            {
                currentWord.Append(c);
            }
        }

        if (currentWord.Length > 0)
        {
            tokens.Add(currentWord.ToString());
        }

        return ApplyBpe(tokens);
    }

    private List<String> ApplyBpe(List<String> words)
    {
        List<String> result = [];

        foreach (String word in words)
        {
            List<String> wordTokens = [.. word.Select(c => c.ToString())];
            Boolean changed = true;

            while (changed && wordTokens.Count > 1)
            {
                changed = false;
                Int32 bestMerge = -1;
                Int32 bestRank = Int32.MaxValue;

                for (Int32 i = 0; i < wordTokens.Count - 1; i++)
                {
                    if (_merges.TryGetValue((wordTokens[i], wordTokens[i + 1]), out Int32 rank))
                    {
                        if (rank < bestRank)
                        {
                            bestRank = rank;
                            bestMerge = i;
                        }
                    }
                }

                if (bestMerge >= 0)
                {
                    String merged = wordTokens[bestMerge] + wordTokens[bestMerge + 1];
                    wordTokens.RemoveAt(bestMerge);
                    wordTokens.RemoveAt(bestMerge);
                    wordTokens.Insert(bestMerge, merged);
                    changed = true;
                }
            }

            result.AddRange(wordTokens);
        }

        return result;
    }
}
