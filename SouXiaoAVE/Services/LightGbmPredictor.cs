// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Runtime.InteropServices;

namespace SouXiaoAVE.Services;

public sealed class LightGbmPredictor : IDisposable
{
    private IntPtr _booster;
    private Boolean _disposed;
    private readonly Int32 _featureCount;
    private Boolean _modelLoaded;

    private const String ModelPath = @"D:\SouXiaoAVE\AIModel\lightgbm_model.txt";

    public Boolean IsModelLoaded => _modelLoaded;

    public LightGbmPredictor(Int32 featureCount = 512)
    {
        _featureCount = featureCount;
        _booster = IntPtr.Zero;
        LoadModel();
    }

    private void LoadModel()
    {
        if (!File.Exists(ModelPath))
        {
            return;
        }

        try
        {
            Int32 result = LGBM_BoosterCreateFromModelfile(ModelPath, out Int32 _, out IntPtr booster);

            if (result == 0 && booster != IntPtr.Zero)
            {
                _booster = booster;
                _modelLoaded = true;
                return;
            }

            String modelString = File.ReadAllText(ModelPath);
            result = LGBM_BoosterLoadModelFromString(modelString, out _, out booster);

            if (result == 0 && booster != IntPtr.Zero)
            {
                _booster = booster;
                _modelLoaded = true;
            }
        }
        catch
        {
            _modelLoaded = false;
        }
    }

    public Single[] Predict(Single[] features)
    {
        if (!_modelLoaded || _booster == IntPtr.Zero || features.Length != _featureCount)
        {
            return [0.5f];
        }

        try
        {
            Int32 predictType = 0;
            Int32 startIteration = 0;
            Int32 numIteration = -1;

            Int64 outLength = 0;
            Single[] result = new Single[1];

            Int32 ret = LGBM_BoosterPredictForMat(
                _booster,
                features,
                0,
                1,
                _featureCount,
                1,
                predictType,
                startIteration,
                numIteration,
                "",
                ref outLength,
                result
            );

            return ret == 0 ? result : [0.5f];
        }
        catch
        {
            return [0.5f];
        }
    }

    public (Double Score, Double Probability, String Label) PredictWithLabel(Single[] features)
    {
        Single[] rawScore = Predict(features);
        Double score = rawScore[0];
        Double probability = Sigmoid(score);
        String label = probability >= 0.5 ? "Malicious" : "Benign";

        return (score, probability, label);
    }

    private static Double Sigmoid(Double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_booster != IntPtr.Zero)
            {
                LGBM_BoosterFree(_booster);
                _booster = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~LightGbmPredictor()
    {
        Dispose();
    }

    [DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 LGBM_BoosterCreateFromModelfile(
        [MarshalAs(UnmanagedType.LPStr)] String filename,
        out Int32 outNumIterations,
        out IntPtr outBooster);

    [DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 LGBM_BoosterLoadModelFromString(
        [MarshalAs(UnmanagedType.LPStr)] String modelStr,
        out Int32 outNumIterations,
        out IntPtr outBooster);

    [DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 LGBM_BoosterPredictForMat(
        IntPtr booster,
        Single[] data,
        Int32 data_type,
        Int32 nrow,
        Int32 ncol,
        Int32 is_row_major,
        Int32 predict_type,
        Int32 start_iteration,
        Int32 num_iteration,
        [MarshalAs(UnmanagedType.LPStr)] String parameter,
        ref Int64 out_len,
        Single[] out_result);

    [DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 LGBM_BoosterFree(IntPtr booster);
}
