// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.IO.Compression;
using System.Text;

namespace SXAVELinker;

public sealed class SXData : IDisposable
{
    private Byte[]? _compressedData;
    private String? _filePath;
    private Boolean _isCompressed;
    private Boolean _disposed;

    public Int64 OriginalSize { get; private set; }
    public Int64 CompressedSize => _compressedData?.Length ?? 0;
    public Boolean IsFilePath => _filePath is not null;
    public Boolean HasData => _compressedData is not null || _filePath is not null;

    public SXData()
    {
    }

    public SXData(Byte[] data)
    {
        SetData(data);
    }

    public SXData(String filePath)
    {
        SetFilePath(filePath);
    }

    public void SetData(Byte[] data)
    {
        ThrowIfDisposed();
        _filePath = null;
        OriginalSize = data.Length;

        if (data.Length > 1024)
        {
            _compressedData = Compress(data);
            _isCompressed = true;
        }
        else
        {
            _compressedData = data;
            _isCompressed = false;
        }
    }

    public void SetFilePath(String filePath)
    {
        ThrowIfDisposed();
        _compressedData = null;
        _isCompressed = false;

        if (File.Exists(filePath))
        {
            _filePath = filePath;
            OriginalSize = new FileInfo(filePath).Length;
        }
        else
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
    }

    public Byte[] GetData()
    {
        ThrowIfDisposed();

        if (_filePath is not null)
        {
            return File.ReadAllBytes(_filePath);
        }

        if (_compressedData is null)
        {
            return [];
        }

        return _isCompressed ? Decompress(_compressedData) : _compressedData;
    }

    public String? GetFilePath()
    {
        return _filePath;
    }

    public async Task<Byte[]> GetDataAsync()
    {
        ThrowIfDisposed();

        if (_filePath is not null)
        {
            return await File.ReadAllBytesAsync(_filePath);
        }

        if (_compressedData is null)
        {
            return [];
        }

        return _isCompressed ? Decompress(_compressedData) : _compressedData;
    }

    public void Clear()
    {
        _compressedData = null;
        _filePath = null;
        _isCompressed = false;
        OriginalSize = 0;
    }

    private static Byte[] Compress(Byte[] data)
    {
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static Byte[] Decompress(Byte[] compressedData)
    {
        using MemoryStream input = new(compressedData);
        using GZipStream gzip = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SXData));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~SXData()
    {
        Dispose();
    }
}
