// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.Security.Cryptography;

namespace ZeroZone;

public sealed class IsolationResult
{
    public Boolean Success { get; init; }
    public String? OutputPath { get; init; }
    public Int64 EncryptedBytes { get; init; }
    public String? Password { get; init; }
    public String? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed class ExtractionResult
{
    public Boolean Success { get; init; }
    public String? OutputDirectory { get; init; }
    public Int32 FileCount { get; init; }
    public Int64 ExtractedBytes { get; init; }
    public String? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed class IsolationService : IDisposable
{
    private const String MagicNumber = "ZZVP";
    private const UInt16 Version = 0x0001;
    private const Int32 AesKeySize = 32;
    private const Int32 AesIVSize = 16;
    private const String FileExtension = ".zzvpkg";

    private Boolean _disposed;

    public static String Extension => FileExtension;

    public async Task<IsolationResult> ImportAsync(String inputPath, String? outputPath = null, String? password = null)
    {
        try
        {
            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                return new IsolationResult { Success = false, ErrorMessage = $"Path not found: {inputPath}" };
            }

            if (String.IsNullOrWhiteSpace(outputPath))
            {
                String baseName = File.Exists(inputPath)
                    ? Path.GetFileNameWithoutExtension(inputPath)
                    : new DirectoryInfo(inputPath).Name;
                outputPath = Path.Combine(
                    Path.GetDirectoryName(inputPath) ?? ".",
                    $"{baseName}{FileExtension}");
            }

            if (!outputPath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                outputPath += FileExtension;
            }

            String generatedPassword = password ?? GenerateRandomPassword();

            Byte[] aesKey = RandomNumberGenerator.GetBytes(AesKeySize);
            Byte[] aesIV = RandomNumberGenerator.GetBytes(AesIVSize);
            Byte[] passwordKey = DeriveKeyFromPassword(generatedPassword, aesKey);
            Byte[] encryptedKey = EncryptAesKey(aesKey, aesIV, passwordKey);

            String tempDir = Path.Combine(Path.GetTempPath(), $"ZeroZone_Import_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                String tempZipPath = Path.Combine(tempDir, "data.zip");

                await using (FileStream zipStream = File.Create(tempZipPath))
                {
                    using ZipArchive zipArchive = ZipArchive.Create();

                    if (File.Exists(inputPath))
                    {
                        String fileName = Path.GetFileName(inputPath);
                        Byte[] fileData = await File.ReadAllBytesAsync(inputPath);
                        zipArchive.AddEntry(fileName, new MemoryStream(fileData));
                    }
                    else
                    {
                        AddDirectoryToZip(zipArchive, inputPath, "");
                    }

                    zipArchive.SaveTo(zipStream, new WriterOptions(CompressionType.Deflate)
                    {
                        LeaveStreamOpen = false
                    });
                }

                Byte[] zipData = await File.ReadAllBytesAsync(tempZipPath);
                Byte[] encryptedData = EncryptAes(zipData, aesKey, aesIV);

                await using (FileStream fs = File.Create(outputPath))
                await using (BinaryWriter writer = new(fs, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes(MagicNumber));
                    writer.Write(Version);
                    writer.Write((UInt16)encryptedKey.Length);
                    writer.Write(encryptedKey);
                    writer.Write(encryptedData);
                }

                return new IsolationResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    EncryptedBytes = encryptedData.Length,
                    Password = generatedPassword
                };
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            return new IsolationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ExtractionResult> ExportAsync(String inputPath, String? outputDir = null, String? password = null)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                return new ExtractionResult { Success = false, ErrorMessage = $"File not found: {inputPath}" };
            }

            if (!inputPath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return new ExtractionResult { Success = false, ErrorMessage = $"Invalid file format. Expected {FileExtension}" };
            }

            if (String.IsNullOrWhiteSpace(outputDir))
            {
                outputDir = Path.GetDirectoryName(inputPath) ?? ".";
            }

            Byte[] fileData = await File.ReadAllBytesAsync(inputPath);

            using MemoryStream ms = new(fileData);
            using BinaryReader reader = new(ms, System.Text.Encoding.UTF8);

            Byte[] magic = reader.ReadBytes(4);
            String magicStr = System.Text.Encoding.ASCII.GetString(magic);

            if (magicStr != MagicNumber)
            {
                return new ExtractionResult { Success = false, ErrorMessage = $"Invalid file format. Expected '{MagicNumber}', got '{magicStr}'" };
            }

            UInt16 version = reader.ReadUInt16();
            if (version > Version)
            {
                return new ExtractionResult { Success = false, ErrorMessage = $"Unsupported version: {version}" };
            }

            UInt16 encryptedKeyLength = reader.ReadUInt16();
            Byte[] encryptedKey = reader.ReadBytes(encryptedKeyLength);
            Byte[] encryptedData = reader.ReadBytes((Int32)(ms.Length - ms.Position));

            Byte[] passwordKey = DeriveKeyFromPassword(password, encryptedKey.Take(AesKeySize).ToArray());
            Byte[] aesKey = DecryptAesKey(encryptedKey, passwordKey, out Byte[] aesIV);
            Byte[] zipData = DecryptAes(encryptedData, aesKey, aesIV);

            String tempDir = Path.Combine(Path.GetTempPath(), $"ZeroZone_Export_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                String tempZipPath = Path.Combine(tempDir, "data.zip");
                await File.WriteAllBytesAsync(tempZipPath, zipData);

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                Int32 fileCount = 0;
                await using (FileStream zipStream = File.OpenRead(tempZipPath))
                {
                    using IArchive archive = ArchiveFactory.Open(zipStream);
                    archive.WriteToDirectory(outputDir, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                    fileCount = archive.Entries.Count(e => !e.IsDirectory);
                }

                return new ExtractionResult
                {
                    Success = true,
                    OutputDirectory = outputDir,
                    FileCount = fileCount,
                    ExtractedBytes = zipData.Length
                };
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            return new ExtractionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public Boolean ValidatePackage(String filePath)
    {
        if (!File.Exists(filePath))
            return false;

        if (!filePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            using FileStream fs = File.OpenRead(filePath);
            using BinaryReader reader = new(fs);

            Byte[] magic = reader.ReadBytes(4);
            String magicStr = System.Text.Encoding.ASCII.GetString(magic);

            return magicStr == MagicNumber;
        }
        catch
        {
            return false;
        }
    }

    public static String GenerateRandomPassword()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private void AddDirectoryToZip(ZipArchive archive, String directoryPath, String entryPrefix)
    {
        foreach (String file in Directory.GetFiles(directoryPath))
        {
            String fileName = Path.GetFileName(file);
            String entryName = String.IsNullOrEmpty(entryPrefix) ? fileName : $"{entryPrefix}/{fileName}";
            Byte[] fileData = File.ReadAllBytes(file);
            archive.AddEntry(entryName, new MemoryStream(fileData));
        }

        foreach (String dir in Directory.GetDirectories(directoryPath))
        {
            String dirName = new DirectoryInfo(dir).Name;
            String newPrefix = String.IsNullOrEmpty(entryPrefix) ? dirName : $"{entryPrefix}/{dirName}";
            AddDirectoryToZip(archive, dir, newPrefix);
        }
    }

    private static Byte[] DeriveKeyFromPassword(String? password, Byte[] salt)
    {
        if (String.IsNullOrEmpty(password))
        {
            password = GenerateRandomPassword();
        }

        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, AesKeySize);
    }

    private static Byte[] EncryptAesKey(Byte[] key, Byte[] iv, Byte[] passwordKey)
    {
        using Aes aes = Aes.Create();
        aes.Key = passwordKey;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        Byte[] encryptedKey = encryptor.TransformFinalBlock(key, 0, key.Length);
        Byte[] encryptedIV = aes.IV;

        Byte[] result = new Byte[AesIVSize + encryptedKey.Length];
        Buffer.BlockCopy(encryptedIV, 0, result, 0, AesIVSize);
        Buffer.BlockCopy(encryptedKey, 0, result, AesIVSize, encryptedKey.Length);
        return result;
    }

    private static Byte[] DecryptAesKey(Byte[] encryptedKey, Byte[] passwordKey, out Byte[] iv)
    {
        Byte[] encryptedIV = new Byte[AesIVSize];
        Buffer.BlockCopy(encryptedKey, 0, encryptedIV, 0, AesIVSize);

        Int32 encryptedDataLength = encryptedKey.Length - AesIVSize;
        Byte[] encryptedData = new Byte[encryptedDataLength];
        Buffer.BlockCopy(encryptedKey, AesIVSize, encryptedData, 0, encryptedDataLength);

        using Aes aes = Aes.Create();
        aes.Key = passwordKey;
        aes.IV = encryptedIV;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        Byte[] decryptedKey = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

        iv = new Byte[AesIVSize];
        Buffer.BlockCopy(encryptedKey, encryptedKey.Length - AesIVSize - AesKeySize + AesIVSize, iv, 0, AesIVSize);

        return decryptedKey;
    }

    private static Byte[] EncryptAes(Byte[] data, Byte[] key, Byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    private static Byte[] DecryptAes(Byte[] data, Byte[] key, Byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
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
