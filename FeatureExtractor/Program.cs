// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using Microsoft.Data.Sqlite;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SouXiaoAVE.Services;
using System.Collections.Concurrent;
using System.Text;

namespace FeatureExtractor;

public sealed class Program
{
    private const String DefaultDbPath = @"D:\Dataset\IceZero\ZeroflowDataset.db";
    private const Int32 FeatureCount = 512;
    private const Int32 MaxDegreeOfParallelism = 8;

    private static readonly PeFeatureExtractor _extractor = new();
    private static readonly Object _consoleLock = new();
    private static String _dbPath = DefaultDbPath;

    public static async Task<Int32> Main(String[] args)
    {
        Console.WriteLine("=== PE Feature Extractor ===");
        Console.WriteLine($"Version: 1.3.0");
        Console.WriteLine($"Feature Dimensions: {FeatureCount}");
        Console.WriteLine($"Max Parallelism: {MaxDegreeOfParallelism}");
        Console.WriteLine();

        try
        {
            await RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] {ex.Message}");
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        Console.WriteLine("Select mode:");
        Console.WriteLine("  1. Extract - Extract features to database");
        Console.WriteLine("  2. Clean   - Clean archive (remove non-PE files)");
        Console.WriteLine();
        Console.Write("Mode (1/2): ");

        String? modeInput = Console.ReadLine()?.Trim();
        Boolean cleanMode = modeInput == "2";

        Console.WriteLine();

        if (cleanMode)
        {
            await RunCleanModeAsync();
        }
        else
        {
            await RunExtractModeAsync();
        }
    }

    private static async Task RunCleanModeAsync()
    {
        Console.Write("Input archive path (7z/zip/rar/tar/gz): ");
        String? inputPath = Console.ReadLine()?.Trim('"').Trim();

        if (String.IsNullOrWhiteSpace(inputPath))
        {
            Console.WriteLine("[ERROR] Path cannot be empty.");
            return;
        }

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"[ERROR] File not found: {inputPath}");
            return;
        }

        String ext = Path.GetExtension(inputPath).ToLowerInvariant();
        if (ext is not (".7z" or ".zip" or ".rar" or ".tar" or ".gz"))
        {
            Console.WriteLine($"[ERROR] Unsupported archive format: {ext}");
            return;
        }

        Console.Write("Password (leave empty if no password): ");
        String? password = ReadPassword();

        Console.Write("Output path (leave empty to auto-generate): ");
        String? outputPath = Console.ReadLine()?.Trim('"').Trim();

        if (String.IsNullOrWhiteSpace(outputPath))
        {
            String dir = Path.GetDirectoryName(inputPath) ?? ".";
            String name = Path.GetFileNameWithoutExtension(inputPath);
            outputPath = Path.Combine(dir, $"{name}_cleaned.zip");
        }

        Console.WriteLine();
        Console.WriteLine($"[INFO] Input: {inputPath}");
        Console.WriteLine($"[INFO] Output: {outputPath}");
        Console.WriteLine($"[INFO] Password: {(String.IsNullOrEmpty(password) ? "None" : "***")}");
        Console.WriteLine();

        CleanStats stats = new();

        try
        {
            await CleanArchiveAsync(inputPath, outputPath, password, stats);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Clean failed: {ex.Message}");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=== Clean Complete ===");
        Console.WriteLine($"Total files scanned: {stats.TotalScanned}");
        Console.WriteLine($"PE files kept: {stats.PeFilesKept}");
        Console.WriteLine($"Non-PE removed: {stats.NonPeRemoved}");
        Console.WriteLine($"Errors: {stats.ErrorCount}");
        Console.WriteLine();
        Console.WriteLine($"Original size: {FormatSize(stats.OriginalSize)}");
        Console.WriteLine($"Cleaned size:  {FormatSize(stats.CleanedSize)}");
        Console.WriteLine($"Saved: {FormatSize(stats.OriginalSize - stats.CleanedSize)} ({(Double)stats.CleanedSize / stats.OriginalSize * 100:F1}% of original)");
    }

    private static async Task RunExtractModeAsync()
    {
        Console.Write("Input path (PE file / directory / 7z archive): ");
        String? inputPath = Console.ReadLine()?.Trim('"').Trim();

        if (String.IsNullOrWhiteSpace(inputPath))
        {
            Console.WriteLine("[ERROR] Path cannot be empty.");
            return;
        }

        if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
        {
            Console.WriteLine($"[ERROR] Path not found: {inputPath}");
            return;
        }

        String? password = null;
        if (File.Exists(inputPath))
        {
            String ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext is ".7z" or ".zip" or ".rar")
            {
                Console.Write("Password (leave empty if no password): ");
                password = ReadPassword();
            }
        }

        Console.Write("Input label (malicious/benign/unknown): ");
        String? label = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (String.IsNullOrWhiteSpace(label))
        {
            label = "unknown";
        }

        if (label is not ("malicious" or "benign" or "unknown"))
        {
            Console.WriteLine($"[WARN] Invalid label '{label}', using 'unknown'.");
            label = "unknown";
        }

        Console.Write($"Database path (default: {DefaultDbPath}): ");
        String? dbPath = Console.ReadLine()?.Trim('"').Trim();

        if (String.IsNullOrWhiteSpace(dbPath))
        {
            dbPath = DefaultDbPath;
        }

        _dbPath = dbPath;

        String? dbDir = Path.GetDirectoryName(dbPath);
        if (!String.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            try
            {
                Directory.CreateDirectory(dbDir);
                Console.WriteLine($"[INFO] Created directory: {dbDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create directory: {ex.Message}");
                return;
            }
        }

        await InitializeDatabaseAsync(dbPath);

        Console.WriteLine();
        Console.WriteLine($"[INFO] Processing: {inputPath}");
        Console.WriteLine($"[INFO] Label: {label}");
        Console.WriteLine($"[INFO] Database: {dbPath}");
        Console.WriteLine();

        ProcessingStats stats = new();

        try
        {
            if (File.Exists(inputPath))
            {
                String ext = Path.GetExtension(inputPath).ToLowerInvariant();
                if (ext is ".7z" or ".zip" or ".rar" or ".tar" or ".gz")
                {
                    await ProcessArchiveParallelAsync(inputPath, label, password, stats);
                }
                else
                {
                    await ProcessSingleFileAsync(inputPath, label, stats);
                }
            }
            else if (Directory.Exists(inputPath))
            {
                await ProcessDirectoryParallelAsync(inputPath, label, stats);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Processing failed: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Processing Complete ===");
        Console.WriteLine($"Total files scanned: {stats.TotalScanned}");
        Console.WriteLine($"PE files found: {stats.PeFilesFound}");
        Console.WriteLine($"Successfully extracted: {stats.SuccessCount}");
        Console.WriteLine($"Skipped (non-PE): {stats.SkippedCount}");
        Console.WriteLine($"Errors: {stats.ErrorCount}");
    }

    private static String? ReadPassword()
    {
        StringBuilder sb = new();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
            {
                sb.Append(key.KeyChar);
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
                Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static async Task InitializeDatabaseAsync(String dbPath)
    {
        await using SqliteConnection connection = new($"Data Source={dbPath}");
        await connection.OpenAsync();

        await using (SqliteCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS samples (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sha256 TEXT NOT NULL UNIQUE,
                    file_name TEXT,
                    file_size INTEGER,
                    label TEXT NOT NULL,
                    extracted_at TEXT NOT NULL,
                    source_path TEXT
                );

                CREATE TABLE IF NOT EXISTS features (
                    sample_id INTEGER PRIMARY KEY,
                    feature_0 REAL, feature_1 REAL, feature_2 REAL, feature_3 REAL, feature_4 REAL,
                    feature_5 REAL, feature_6 REAL, feature_7 REAL, feature_8 REAL, feature_9 REAL,
                    feature_10 REAL, feature_11 REAL, feature_12 REAL, feature_13 REAL, feature_14 REAL,
                    feature_15 REAL, feature_16 REAL, feature_17 REAL, feature_18 REAL, feature_19 REAL,
                    feature_20 REAL, feature_21 REAL, feature_22 REAL, feature_23 REAL, feature_24 REAL,
                    feature_25 REAL, feature_26 REAL, feature_27 REAL, feature_28 REAL, feature_29 REAL,
                    feature_30 REAL, feature_31 REAL, feature_32 REAL, feature_33 REAL, feature_34 REAL,
                    feature_35 REAL, feature_36 REAL, feature_37 REAL, feature_38 REAL, feature_39 REAL,
                    feature_40 REAL, feature_41 REAL, feature_42 REAL, feature_43 REAL, feature_44 REAL,
                    feature_45 REAL, feature_46 REAL, feature_47 REAL, feature_48 REAL, feature_49 REAL,
                    feature_50 REAL, feature_51 REAL, feature_52 REAL, feature_53 REAL, feature_54 REAL,
                    feature_55 REAL, feature_56 REAL, feature_57 REAL, feature_58 REAL, feature_59 REAL,
                    feature_60 REAL, feature_61 REAL, feature_62 REAL, feature_63 REAL, feature_64 REAL,
                    feature_65 REAL, feature_66 REAL, feature_67 REAL, feature_68 REAL, feature_69 REAL,
                    feature_70 REAL, feature_71 REAL, feature_72 REAL, feature_73 REAL, feature_74 REAL,
                    feature_75 REAL, feature_76 REAL, feature_77 REAL, feature_78 REAL, feature_79 REAL,
                    feature_80 REAL, feature_81 REAL, feature_82 REAL, feature_83 REAL, feature_84 REAL,
                    feature_85 REAL, feature_86 REAL, feature_87 REAL, feature_88 REAL, feature_89 REAL,
                    feature_90 REAL, feature_91 REAL, feature_92 REAL, feature_93 REAL, feature_94 REAL,
                    feature_95 REAL, feature_96 REAL, feature_97 REAL, feature_98 REAL, feature_99 REAL,
                    feature_100 REAL, feature_101 REAL, feature_102 REAL, feature_103 REAL, feature_104 REAL,
                    feature_105 REAL, feature_106 REAL, feature_107 REAL, feature_108 REAL, feature_109 REAL,
                    feature_110 REAL, feature_111 REAL, feature_112 REAL, feature_113 REAL, feature_114 REAL,
                    feature_115 REAL, feature_116 REAL, feature_117 REAL, feature_118 REAL, feature_119 REAL,
                    feature_120 REAL, feature_121 REAL, feature_122 REAL, feature_123 REAL, feature_124 REAL,
                    feature_125 REAL, feature_126 REAL, feature_127 REAL, feature_128 REAL, feature_129 REAL,
                    feature_130 REAL, feature_131 REAL, feature_132 REAL, feature_133 REAL, feature_134 REAL,
                    feature_135 REAL, feature_136 REAL, feature_137 REAL, feature_138 REAL, feature_139 REAL,
                    feature_140 REAL, feature_141 REAL, feature_142 REAL, feature_143 REAL, feature_144 REAL,
                    feature_145 REAL, feature_146 REAL, feature_147 REAL, feature_148 REAL, feature_149 REAL,
                    feature_150 REAL, feature_151 REAL, feature_152 REAL, feature_153 REAL, feature_154 REAL,
                    feature_155 REAL, feature_156 REAL, feature_157 REAL, feature_158 REAL, feature_159 REAL,
                    feature_160 REAL, feature_161 REAL, feature_162 REAL, feature_163 REAL, feature_164 REAL,
                    feature_165 REAL, feature_166 REAL, feature_167 REAL, feature_168 REAL, feature_169 REAL,
                    feature_170 REAL, feature_171 REAL, feature_172 REAL, feature_173 REAL, feature_174 REAL,
                    feature_175 REAL, feature_176 REAL, feature_177 REAL, feature_178 REAL, feature_179 REAL,
                    feature_180 REAL, feature_181 REAL, feature_182 REAL, feature_183 REAL, feature_184 REAL,
                    feature_185 REAL, feature_186 REAL, feature_187 REAL, feature_188 REAL, feature_189 REAL,
                    feature_190 REAL, feature_191 REAL, feature_192 REAL, feature_193 REAL, feature_194 REAL,
                    feature_195 REAL, feature_196 REAL, feature_197 REAL, feature_198 REAL, feature_199 REAL,
                    feature_200 REAL, feature_201 REAL, feature_202 REAL, feature_203 REAL, feature_204 REAL,
                    feature_205 REAL, feature_206 REAL, feature_207 REAL, feature_208 REAL, feature_209 REAL,
                    feature_210 REAL, feature_211 REAL, feature_212 REAL, feature_213 REAL, feature_214 REAL,
                    feature_215 REAL, feature_216 REAL, feature_217 REAL, feature_218 REAL, feature_219 REAL,
                    feature_220 REAL, feature_221 REAL, feature_222 REAL, feature_223 REAL, feature_224 REAL,
                    feature_225 REAL, feature_226 REAL, feature_227 REAL, feature_228 REAL, feature_229 REAL,
                    feature_230 REAL, feature_231 REAL, feature_232 REAL, feature_233 REAL, feature_234 REAL,
                    feature_235 REAL, feature_236 REAL, feature_237 REAL, feature_238 REAL, feature_239 REAL,
                    feature_240 REAL, feature_241 REAL, feature_242 REAL, feature_243 REAL, feature_244 REAL,
                    feature_245 REAL, feature_246 REAL, feature_247 REAL, feature_248 REAL, feature_249 REAL,
                    feature_250 REAL, feature_251 REAL, feature_252 REAL, feature_253 REAL, feature_254 REAL,
                    feature_255 REAL, feature_256 REAL, feature_257 REAL, feature_258 REAL, feature_259 REAL,
                    feature_260 REAL, feature_261 REAL, feature_262 REAL, feature_263 REAL, feature_264 REAL,
                    feature_265 REAL, feature_266 REAL, feature_267 REAL, feature_268 REAL, feature_269 REAL,
                    feature_270 REAL, feature_271 REAL, feature_272 REAL, feature_273 REAL, feature_274 REAL,
                    feature_275 REAL, feature_276 REAL, feature_277 REAL, feature_278 REAL, feature_279 REAL,
                    feature_280 REAL, feature_281 REAL, feature_282 REAL, feature_283 REAL, feature_284 REAL,
                    feature_285 REAL, feature_286 REAL, feature_287 REAL, feature_288 REAL, feature_289 REAL,
                    feature_290 REAL, feature_291 REAL, feature_292 REAL, feature_293 REAL, feature_294 REAL,
                    feature_295 REAL, feature_296 REAL, feature_297 REAL, feature_298 REAL, feature_299 REAL,
                    feature_300 REAL, feature_301 REAL, feature_302 REAL, feature_303 REAL, feature_304 REAL,
                    feature_305 REAL, feature_306 REAL, feature_307 REAL, feature_308 REAL, feature_309 REAL,
                    feature_310 REAL, feature_311 REAL, feature_312 REAL, feature_313 REAL, feature_314 REAL,
                    feature_315 REAL, feature_316 REAL, feature_317 REAL, feature_318 REAL, feature_319 REAL,
                    feature_320 REAL, feature_321 REAL, feature_322 REAL, feature_323 REAL, feature_324 REAL,
                    feature_325 REAL, feature_326 REAL, feature_327 REAL, feature_328 REAL, feature_329 REAL,
                    feature_330 REAL, feature_331 REAL, feature_332 REAL, feature_333 REAL, feature_334 REAL,
                    feature_335 REAL, feature_336 REAL, feature_337 REAL, feature_338 REAL, feature_339 REAL,
                    feature_340 REAL, feature_341 REAL, feature_342 REAL, feature_343 REAL, feature_344 REAL,
                    feature_345 REAL, feature_346 REAL, feature_347 REAL, feature_348 REAL, feature_349 REAL,
                    feature_350 REAL, feature_351 REAL, feature_352 REAL, feature_353 REAL, feature_354 REAL,
                    feature_355 REAL, feature_356 REAL, feature_357 REAL, feature_358 REAL, feature_359 REAL,
                    feature_360 REAL, feature_361 REAL, feature_362 REAL, feature_363 REAL, feature_364 REAL,
                    feature_365 REAL, feature_366 REAL, feature_367 REAL, feature_368 REAL, feature_369 REAL,
                    feature_370 REAL, feature_371 REAL, feature_372 REAL, feature_373 REAL, feature_374 REAL,
                    feature_375 REAL, feature_376 REAL, feature_377 REAL, feature_378 REAL, feature_379 REAL,
                    feature_380 REAL, feature_381 REAL, feature_382 REAL, feature_383 REAL, feature_384 REAL,
                    feature_385 REAL, feature_386 REAL, feature_387 REAL, feature_388 REAL, feature_389 REAL,
                    feature_390 REAL, feature_391 REAL, feature_392 REAL, feature_393 REAL, feature_394 REAL,
                    feature_395 REAL, feature_396 REAL, feature_397 REAL, feature_398 REAL, feature_399 REAL,
                    feature_400 REAL, feature_401 REAL, feature_402 REAL, feature_403 REAL, feature_404 REAL,
                    feature_405 REAL, feature_406 REAL, feature_407 REAL, feature_408 REAL, feature_409 REAL,
                    feature_410 REAL, feature_411 REAL, feature_412 REAL, feature_413 REAL, feature_414 REAL,
                    feature_415 REAL, feature_416 REAL, feature_417 REAL, feature_418 REAL, feature_419 REAL,
                    feature_420 REAL, feature_421 REAL, feature_422 REAL, feature_423 REAL, feature_424 REAL,
                    feature_425 REAL, feature_426 REAL, feature_427 REAL, feature_428 REAL, feature_429 REAL,
                    feature_430 REAL, feature_431 REAL, feature_432 REAL, feature_433 REAL, feature_434 REAL,
                    feature_435 REAL, feature_436 REAL, feature_437 REAL, feature_438 REAL, feature_439 REAL,
                    feature_440 REAL, feature_441 REAL, feature_442 REAL, feature_443 REAL, feature_444 REAL,
                    feature_445 REAL, feature_446 REAL, feature_447 REAL, feature_448 REAL, feature_449 REAL,
                    feature_450 REAL, feature_451 REAL, feature_452 REAL, feature_453 REAL, feature_454 REAL,
                    feature_455 REAL, feature_456 REAL, feature_457 REAL, feature_458 REAL, feature_459 REAL,
                    feature_460 REAL, feature_461 REAL, feature_462 REAL, feature_463 REAL, feature_464 REAL,
                    feature_465 REAL, feature_466 REAL, feature_467 REAL, feature_468 REAL, feature_469 REAL,
                    feature_470 REAL, feature_471 REAL, feature_472 REAL, feature_473 REAL, feature_474 REAL,
                    feature_475 REAL, feature_476 REAL, feature_477 REAL, feature_478 REAL, feature_479 REAL,
                    feature_480 REAL, feature_481 REAL, feature_482 REAL, feature_483 REAL, feature_484 REAL,
                    feature_485 REAL, feature_486 REAL, feature_487 REAL, feature_488 REAL, feature_489 REAL,
                    feature_490 REAL, feature_491 REAL, feature_492 REAL, feature_493 REAL, feature_494 REAL,
                    feature_495 REAL, feature_496 REAL, feature_497 REAL, feature_498 REAL, feature_499 REAL,
                    feature_500 REAL, feature_501 REAL, feature_502 REAL, feature_503 REAL, feature_504 REAL,
                    feature_505 REAL, feature_506 REAL, feature_507 REAL, feature_508 REAL, feature_509 REAL,
                    feature_510 REAL, feature_511 REAL,
                    FOREIGN KEY (sample_id) REFERENCES samples(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_samples_sha256 ON samples(sha256);
                CREATE INDEX IF NOT EXISTS idx_samples_label ON samples(label);
            ";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task ProcessDirectoryParallelAsync(String directoryPath, String label, ProcessingStats stats)
    {
        LogInfo($"Scanning directory: {directoryPath}");
        Console.WriteLine();

        IEnumerable<String> files;
        try
        {
            files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogError($"Access denied to directory: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            LogError($"Failed to enumerate directory: {ex.Message}");
            return;
        }

        ConcurrentBag<String> fileBag = [.. files];
        Int32 totalFiles = fileBag.Count;
        Int32 processedCount = 0;

        LogInfo($"Found {totalFiles} files, processing with {MaxDegreeOfParallelism} threads...");
        Console.WriteLine();

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(fileBag, options, file =>
            {
                try
                {
                    ProcessSingleFileSync(file, label, stats);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to process {file}: {ex.Message}");
                    Interlocked.Increment(ref stats.ErrorCount);
                }

                Int32 current = Interlocked.Increment(ref processedCount);
                if (current % 100 == 0 || current == totalFiles)
                {
                    LogProgress(current, totalFiles, stats);
                }
            });
        });
    }

    private static void ProcessSingleFileSync(String filePath, String label, ProcessingStats stats)
    {
        Interlocked.Increment(ref stats.TotalScanned);

        try
        {
            Byte[] fileBytes = File.ReadAllBytes(filePath);

            if (!IsValidPeFile(fileBytes))
            {
                Interlocked.Increment(ref stats.SkippedCount);
                return;
            }

            Interlocked.Increment(ref stats.PeFilesFound);

            String sha256 = ComputeSha256(fileBytes);
            String fileName = Path.GetFileName(filePath);
            Int64 fileSize = fileBytes.Length;

            if (IsDuplicateSync(sha256))
            {
                Interlocked.Increment(ref stats.SkippedCount);
                return;
            }

            Double[] features = ExtractFeatures(fileBytes);
            if (features.Length != FeatureCount)
            {
                Interlocked.Increment(ref stats.ErrorCount);
                return;
            }

            SaveToDatabaseSync(sha256, fileName, fileSize, label, filePath, features);
            Interlocked.Increment(ref stats.SuccessCount);
        }
        catch (UnauthorizedAccessException)
        {
            Interlocked.Increment(ref stats.ErrorCount);
        }
        catch (IOException)
        {
            Interlocked.Increment(ref stats.ErrorCount);
        }
        catch
        {
            Interlocked.Increment(ref stats.ErrorCount);
        }
    }

    private static async Task ProcessArchiveParallelAsync(String archivePath, String label, String? password, ProcessingStats stats)
    {
        LogInfo($"Extracting archive: {archivePath}");
        Console.WriteLine();

        ReaderOptions readerOptions = new();
        if (!String.IsNullOrEmpty(password))
        {
            readerOptions.Password = password;
        }

        using IArchive archive = ArchiveFactory.Open(archivePath, readerOptions);

        List<IArchiveEntry> entries = archive.Entries
            .Where(e => !e.IsDirectory)
            .ToList();

        Int32 totalEntries = entries.Count;
        Int32 processedCount = 0;

        LogInfo($"Found {totalEntries} entries, processing with {MaxDegreeOfParallelism} threads...");
        Console.WriteLine();

        ConcurrentBag<ArchiveEntryData> entryDataBag = [];

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(entries, options, entry =>
            {
                Interlocked.Increment(ref stats.TotalScanned);

                try
                {
                    using MemoryStream ms = new();
                    entry.WriteTo(ms);
                    Byte[] fileBytes = ms.ToArray();

                    if (IsValidPeFile(fileBytes))
                    {
                        String entryName = entry.Key ?? $"unknown_{Guid.NewGuid():N}";
                        entryDataBag.Add(new ArchiveEntryData(entryName, fileBytes));
                    }
                    else
                    {
                        Interlocked.Increment(ref stats.SkippedCount);
                    }
                }
                catch (CryptographicException)
                {
                    LogError($"Wrong password or encrypted entry: {entry.Key}");
                    Interlocked.Increment(ref stats.ErrorCount);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to extract entry: {ex.Message}");
                    Interlocked.Increment(ref stats.ErrorCount);
                }

                Int32 current = Interlocked.Increment(ref processedCount);
                if (current % 100 == 0 || current == totalEntries)
                {
                    LogProgress(current, totalEntries, stats);
                }
            });
        });

        LogInfo($"Extracted {entryDataBag.Count} PE files, saving to database...");
        Console.WriteLine();

        Int32 savedCount = 0;
        Int32 totalToSave = entryDataBag.Count;

        ParallelOptions saveOptions = new()
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(entryDataBag, saveOptions, entryData =>
            {
                Interlocked.Increment(ref stats.PeFilesFound);

                try
                {
                    String sha256 = ComputeSha256(entryData.FileBytes);
                    Int64 fileSize = entryData.FileBytes.Length;

                    if (IsDuplicateSync(sha256))
                    {
                        Interlocked.Increment(ref stats.SkippedCount);
                        return;
                    }

                    Double[] features = ExtractFeatures(entryData.FileBytes);
                    if (features.Length != FeatureCount)
                    {
                        Interlocked.Increment(ref stats.ErrorCount);
                        return;
                    }

                    SaveToDatabaseSync(sha256, entryData.EntryName, fileSize, label, archivePath, features);
                    Interlocked.Increment(ref stats.SuccessCount);
                }
                catch
                {
                    Interlocked.Increment(ref stats.ErrorCount);
                }

                Int32 current = Interlocked.Increment(ref savedCount);
                if (current % 100 == 0 || current == totalToSave)
                {
                    LogProgress(current, totalToSave, stats);
                }
            });
        });
    }

    private static async Task ProcessSingleFileAsync(String filePath, String label, ProcessingStats stats)
    {
        stats.TotalScanned++;

        try
        {
            Byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            if (!IsValidPeFile(fileBytes))
            {
                LogInfo($"Not a valid PE file: {Path.GetFileName(filePath)}");
                stats.SkippedCount++;
                return;
            }

            stats.PeFilesFound++;

            String sha256 = ComputeSha256(fileBytes);
            String fileName = Path.GetFileName(filePath);
            Int64 fileSize = fileBytes.Length;

            if (IsDuplicateSync(sha256))
            {
                LogInfo($"Duplicate file: {fileName} ({sha256[..8]}...)");
                stats.SkippedCount++;
                return;
            }

            LogInfo($"Extracting: {fileName} ({FormatSize(fileSize)})");

            Double[] features = ExtractFeatures(fileBytes);
            if (features.Length != FeatureCount)
            {
                LogError($"Invalid feature count: {features.Length}");
                stats.ErrorCount++;
                return;
            }

            SaveToDatabaseSync(sha256, fileName, fileSize, label, filePath, features);
            stats.SuccessCount++;
        }
        catch (UnauthorizedAccessException ex)
        {
            LogError($"Access denied: {ex.Message}");
            stats.ErrorCount++;
        }
        catch (IOException ex)
        {
            LogError($"IO error: {ex.Message}");
            stats.ErrorCount++;
        }
        catch (Exception ex)
        {
            LogError(ex.Message);
            stats.ErrorCount++;
        }
    }

    private static async Task CleanArchiveAsync(String inputPath, String outputPath, String? password, CleanStats stats)
    {
        LogInfo("Extracting archive...");

        ReaderOptions readerOptions = new();
        if (!String.IsNullOrEmpty(password))
        {
            readerOptions.Password = password;
        }

        using IArchive archive = ArchiveFactory.Open(inputPath, readerOptions);
        ConcurrentDictionary<String, Byte[]> peFiles = new();

        List<IArchiveEntry> entries = archive.Entries
            .Where(e => !e.IsDirectory)
            .ToList();

        Int32 totalEntries = entries.Count;
        Int32 processedCount = 0;

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(entries, options, entry =>
            {
                Interlocked.Increment(ref stats.TotalScanned);

                try
                {
                    using MemoryStream ms = new();
                    entry.WriteTo(ms);
                    Byte[] fileBytes = ms.ToArray();

                    if (IsValidPeFile(fileBytes))
                    {
                        String entryName = entry.Key ?? $"unknown_{stats.PeFilesKept}";
                        peFiles[entryName] = fileBytes;
                        Interlocked.Increment(ref stats.PeFilesKept);
                        Interlocked.Add(ref stats.OriginalSize, fileBytes.Length);
                        Interlocked.Add(ref stats.CleanedSize, fileBytes.Length);

                        Int32 current = stats.PeFilesKept;
                        if (current % 100 == 0)
                        {
                            LogInfo($"Found {current} PE files...");
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref stats.NonPeRemoved);
                        Interlocked.Add(ref stats.OriginalSize, fileBytes.Length);
                    }
                }
                catch (CryptographicException)
                {
                    LogError($"Wrong password or encrypted entry: {entry.Key}");
                    Interlocked.Increment(ref stats.ErrorCount);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to process entry: {ex.Message}");
                    Interlocked.Increment(ref stats.ErrorCount);
                }

                Int32 current2 = Interlocked.Increment(ref processedCount);
                if (current2 % 500 == 0 || current2 == totalEntries)
                {
                    LogProgress(current2, totalEntries);
                }
            });
        });

        LogInfo($"Found {stats.PeFilesKept} PE files, removing {stats.NonPeRemoved} non-PE files");
        LogInfo($"Creating cleaned archive: {outputPath}");

        await using (FileStream fs = File.Create(outputPath))
        {
            using ZipArchive zipArchive = ZipArchive.Create();

            foreach (KeyValuePair<String, Byte[]> kvp in peFiles)
            {
                zipArchive.AddEntry(kvp.Key, new MemoryStream(kvp.Value));
            }

            zipArchive.SaveTo(fs, new WriterOptions(CompressionType.Deflate)
            {
                LeaveStreamOpen = false
            });
        }

        LogInfo("Archive created successfully (no password)");
    }

    private static Boolean IsValidPeFile(Byte[] data)
    {
        if (data.Length < 2)
            return false;

        return data[0] == 0x4D && data[1] == 0x5A;
    }

    private static String ComputeSha256(Byte[] data)
    {
        using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        Byte[] hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static Double[] ExtractFeatures(Byte[] fileBytes)
    {
        SouXiaoAVE.Models.FeatureVector features = _extractor.Extract(fileBytes);
        return features.Features;
    }

    private static Boolean IsDuplicateSync(String sha256)
    {
        using SqliteConnection connection = new($"Data Source={_dbPath}");
        connection.Open();

        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM samples WHERE sha256 = @sha256";
        cmd.Parameters.AddWithValue("@sha256", sha256);

        Object? result = cmd.ExecuteScalar();
        return result is not null;
    }

    private static void SaveToDatabaseSync(String sha256, String fileName, Int64 fileSize, String label, String sourcePath, Double[] features)
    {
        using SqliteConnection connection = new($"Data Source={_dbPath}");
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            Int64 sampleId;

            using (SqliteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO samples (sha256, file_name, file_size, label, extracted_at, source_path)
                    VALUES (@sha256, @fileName, @fileSize, @label, @extractedAt, @sourcePath);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@sha256", sha256);
                cmd.Parameters.AddWithValue("@fileName", fileName);
                cmd.Parameters.AddWithValue("@fileSize", fileSize);
                cmd.Parameters.AddWithValue("@label", label);
                cmd.Parameters.AddWithValue("@extractedAt", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("@sourcePath", sourcePath);

                sampleId = Convert.ToInt64(cmd.ExecuteScalar());
            }

            using (SqliteCommand cmd = connection.CreateCommand())
            {
                List<String> columns = [];
                List<String> values = [];
                for (Int32 i = 0; i < FeatureCount; i++)
                {
                    columns.Add($"feature_{i}");
                    values.Add($"@f{i}");
                    cmd.Parameters.AddWithValue($"@f{i}", features[i]);
                }

                cmd.CommandText = $"INSERT INTO features (sample_id, {String.Join(", ", columns)}) VALUES (@sampleId, {String.Join(", ", values)})";
                cmd.Parameters.AddWithValue("@sampleId", sampleId);

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void LogInfo(String message)
    {
        lock (_consoleLock)
        {
            Console.WriteLine($"[INFO] {message}");
        }
    }

    private static void LogError(String message)
    {
        lock (_consoleLock)
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }

    private static void LogProgress(Int32 current, Int32 total, ProcessingStats stats)
    {
        lock (_consoleLock)
        {
            Console.WriteLine($"[PROGRESS] {current}/{total} - Success: {stats.SuccessCount}, Skipped: {stats.SkippedCount}, Errors: {stats.ErrorCount}");
        }
    }

    private static void LogProgress(Int32 current, Int32 total)
    {
        lock (_consoleLock)
        {
            Double percent = (Double)current / total * 100;
            Console.WriteLine($"[PROGRESS] {current}/{total} ({percent:F1}%)");
        }
    }

    private static String FormatSize(Int64 bytes)
    {
        String[] units = ["B", "KB", "MB", "GB"];
        Double size = bytes;
        Int32 unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F1} {units[unitIndex]}";
    }
}

public sealed class ProcessingStats
{
    public Int32 TotalScanned;
    public Int32 PeFilesFound;
    public Int32 SuccessCount;
    public Int32 SkippedCount;
    public Int32 ErrorCount;
}

public sealed class CleanStats
{
    public Int32 TotalScanned;
    public Int32 PeFilesKept;
    public Int32 NonPeRemoved;
    public Int32 ErrorCount;
    public Int64 OriginalSize;
    public Int64 CleanedSize;
}

public sealed class ArchiveEntryData
{
    public String EntryName { get; }
    public Byte[] FileBytes { get; }

    public ArchiveEntryData(String entryName, Byte[] fileBytes)
    {
        EntryName = entryName;
        FileBytes = fileBytes;
    }
}
