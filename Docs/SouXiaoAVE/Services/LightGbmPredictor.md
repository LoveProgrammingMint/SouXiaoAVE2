# LightGbmPredictor Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/LightGbmPredictor.cs`  
**Type**: Sealed Class  
**Implements**: `IDisposable`

## Overview

LightGBM model inference class using P/Invoke to call native lib_lightgbm dynamic library for efficient prediction. Supports loading models from both files and strings.

## Properties

| Property Name | Type | Read-only | Description |
|---------------|------|-----------|-------------|
| IsModelLoaded | Boolean | Yes | Whether model loaded successfully |

## Constants

| Constant Name | Value | Description |
|---------------|-------|-------------|
| ModelPath | `D:\SouXiaoAVE\AIModel\lightgbm_model.txt` | Model file path |

## Constructor

### LightGbmPredictor(Int32 featureCount = 512)

Create a predictor instance.

**Parameters**:
- `featureCount`: Int32 - Feature dimension count, default 512

**Internal Flow**:
1. Store feature dimension
2. Initialize booster handle to null
3. Call LoadModel to load the model

```csharp
LightGbmPredictor predictor = new(512);
Console.WriteLine($"Model loaded: {predictor.IsModelLoaded}");
```

## Methods

### Predict(Single[] features)

Execute prediction and return raw score.

**Parameters**:
- `features`: Single[] - Feature array (must be 512 dimensions)

**Returns**: Single[] - Prediction result array (usually single value)

**Internal Flow**:
1. Check if model is loaded
2. Check if feature dimensions match
3. Call LGBM_BoosterPredictForMat
4. Return prediction result

**Exception Cases**:
- Returns [0.5f] when model not loaded
- Returns [0.5f] when feature dimensions don't match
- Returns [0.5f] when prediction fails

```csharp
Single[] features = new Single[512];
// ... fill features ...
Single[] result = predictor.Predict(features);
Console.WriteLine($"Raw score: {result[0]}");
```

### PredictWithLabel(Single[] features)

Execute prediction and return labeled result.

**Parameters**:
- `features`: Single[] - Feature array

**Returns**: (Double Score, Double Probability, String Label) - Tuple containing:
- `Score`: Raw prediction score
- `Probability`: Probability value after Sigmoid transformation
- `Label`: "Malicious" (probability >= 0.5) or "Benign"

**Internal Flow**:
1. Call Predict to get raw score
2. Use Sigmoid function to convert to probability
3. Determine label based on probability threshold

```csharp
(Double score, Double probability, String label) = predictor.PredictWithLabel(features);

Console.WriteLine($"Score: {score:F4}");
Console.WriteLine($"Probability: {probability:P2}");
Console.WriteLine($"Label: {label}");
```

### Dispose()

Release resources.

**Internal Flow**:
1. Check if already disposed
2. Call LGBM_BoosterFree to release booster
3. Mark as disposed

```csharp
predictor.Dispose();
```

## P/Invoke Declarations

### LGBM_BoosterCreateFromModelfile

Create Booster from model file.

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterCreateFromModelfile(
    [MarshalAs(UnmanagedType.LPStr)] String filename,
    out Int32 outNumIterations,
    out IntPtr outBooster);
```

### LGBM_BoosterLoadModelFromString

Create Booster from model string.

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterLoadModelFromString(
    [MarshalAs(UnmanagedType.LPStr)] String modelStr,
    out Int32 outNumIterations,
    out IntPtr outBooster);
```

### LGBM_BoosterPredictForMat

Execute matrix prediction.

```csharp
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
```

### LGBM_BoosterFree

Free Booster.

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterFree(IntPtr booster);
```

## Usage Examples

### Basic Prediction

```csharp
using LightGbmPredictor predictor = new(512);

if (!predictor.IsModelLoaded)
{
    Console.WriteLine("Model load failed");
    return;
}

// Prepare features
Single[] features = new Single[512];
for (Int32 i = 0; i < 512; i++)
{
    features[i] = (Single)Random.Shared.NextDouble();
}

// Predict
(Double score, Double probability, String label) = predictor.PredictWithLabel(features);

Console.WriteLine($"Prediction result: {label}");
Console.WriteLine($"Malware probability: {probability:P2}");
```

### Integration with PeFeatureExtractor

```csharp
using LightGbmPredictor predictor = new(512);
PeFeatureExtractor extractor = new();

Byte[] fileBytes = File.ReadAllBytes(@"C:\test.exe");
FeatureVector features = extractor.Extract(fileBytes);

// Convert to Single array
Single[] featureArray = new Single[FeatureVector.TotalDimensions];
for (Int32 i = 0; i < features.Features.Length; i++)
{
    featureArray[i] = (Single)features.Features[i];
}

// Predict
var result = predictor.PredictWithLabel(featureArray);
Console.WriteLine($"File analysis result: {result.Label} ({result.Probability:P2})");
```

### Batch Prediction

```csharp
using LightGbmPredictor predictor = new(512);
PeFeatureExtractor extractor = new();

String[] files = Directory.GetFiles(@"C:\Samples", "*.exe");

foreach (String file in files)
{
    try
    {
        FeatureVector features = extractor.Extract(file);
        Single[] arr = features.Features.Select(f => (Single)f).ToArray();
        
        var result = predictor.PredictWithLabel(arr);
        
        Console.WriteLine($"{Path.GetFileName(file)}: {result.Label} ({result.Probability:P2})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(file)}: Error - {ex.Message}");
    }
}
```

## Error Handling

```csharp
LightGbmPredictor predictor = new(512);

// Check model loading
if (!predictor.IsModelLoaded)
{
    // Possible reasons:
    // 1. Model file does not exist
    // 2. Model file format error
    // 3. lib_lightgbm.dll not found
    Console.WriteLine("Warning: Model not loaded, prediction will return default value");
}

// Check dimensions during prediction
Single[] wrongFeatures = new Single[256]; // Wrong dimensions
Single[] result = predictor.Predict(wrongFeatures);
// Returns [0.5f] instead of throwing exception
```

## Notes

- Requires lib_lightgbm.dll in system PATH or application directory
- Model file path is hardcoded, should be configurable in production
- Returns default value instead of throwing exception on prediction failure
- Use `using` statement to ensure resource disposal
- Feature dimensions must be specified at construction and match the model
- Sigmoid function is used to convert raw score to probability
