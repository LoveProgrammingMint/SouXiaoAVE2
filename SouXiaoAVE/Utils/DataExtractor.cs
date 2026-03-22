// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.Utils;

internal sealed class DataExtractor : IDisposable
{
    private IDataExtraction? _extractor;
    private IDataPreprocessor? _preprocessor;
    private Boolean _disposed;

    public String ExtractorName => _extractor?.ExtractorName ?? String.Empty;

    public DataExtractor()
    {
    }

    public DataExtractor(IDataExtraction extractor, IDataPreprocessor? preprocessor = null)
    {
        _extractor = extractor;
        _preprocessor = preprocessor;
    }

    public void SetExtractor(IDataExtraction extractor) => _extractor = extractor;

    public void SetPreprocessor(IDataPreprocessor preprocessor) => _preprocessor = preprocessor;

    public DataExtractionResult Extract(String filePath)
    {
        if (_extractor is null)
        {
            return new DataExtractionResult();
        }

        List<Single> features = _extractor.Extract(filePath);

        if (_preprocessor is not null)
        {
            features = _preprocessor.Preprocess(features);
        }

        return new DataExtractionResult(_extractor.ExtractorName, features, filePath);
    }

    public async Task<DataExtractionResult> ExtractAsync(String filePath)
    {
        if (_extractor is null)
        {
            return new DataExtractionResult();
        }

        List<Single> features = await _extractor.ExtractAsync(filePath);

        if (_preprocessor is not null)
        {
            features = _preprocessor.Preprocess(features);
        }

        return new DataExtractionResult(_extractor.ExtractorName, features, filePath);
    }

    public static DataExtractionResult QuickExtract(IDataExtraction extractor, String filePath, IDataPreprocessor? preprocessor = null)
    {
        List<Single> features = extractor.Extract(filePath);

        if (preprocessor is not null)
        {
            features = preprocessor.Preprocess(features);
        }

        return new DataExtractionResult(extractor.ExtractorName, features, filePath);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _extractor?.Dispose();
        _preprocessor?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
