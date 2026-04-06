# SXObject 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXObject.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

层级对象容器，支持树形结构组织、元数据存储和数据绑定。用于构建复杂的分析结果对象树。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| ID | Guid | 是 | 对象唯一标识 |
| Type | SXType | 是 | 对象类型 |
| Name | String? | 否 | 对象名称 |
| Data | SXData? | 否 | 关联数据 |
| Metadata | `Dictionary<String, Object>` | 是 | 元数据字典 |
| Children | `Dictionary<String, SXObject>` | 是 | 子对象字典 |
| Parent | SXObject? | 是 | 父对象引用 |
| CreatedAt | DateTime | 是 | 创建时间（UTC） |
| ModifiedAt | DateTime? | 是 | 最后修改时间（UTC） |

## 构造函数

### SXObject(SXType type)

创建指定类型的对象。

**参数**:
- `type`: SXType - 对象类型

```csharp
SXObject obj = new(SXType.Report);
```

### SXObject(SXType type, String name)

创建指定类型和名称的对象。

**参数**:
- `type`: SXType - 对象类型
- `name`: String - 对象名称

```csharp
SXObject obj = new(SXType.Report, "AnalysisResult");
```

## 方法

### SetMetadata(String key, Object value)

设置元数据。

**参数**:
- `key`: String - 元数据键
- `value`: Object - 元数据值

**内部流程**:
1. 存入Metadata字典
2. 更新ModifiedAt时间戳

**输出**: 无返回值

```csharp
obj.SetMetadata("Author", "SouXiaoAVE");
obj.SetMetadata("Version", "1.0.0");
obj.SetMetadata("Confidence", 0.95);
```

### `GetMetadata<T>`(String key)

获取元数据。

**参数**:
- `key`: String - 元数据键

**输出**: `T?` - 类型化的元数据值，不存在或类型不匹配返回 `default`

```csharp
String? author = obj.GetMetadata<String>("Author");
Double confidence = obj.GetMetadata<Double>("Confidence");
```

### AddChild(String name, SXObject child)

添加子对象。

**参数**:
- `name`: String - 子对象名称（键）
- `child`: SXObject - 子对象实例

**内部流程**:
1. 设置child的Parent为当前对象
2. 添加到Children字典
3. 更新ModifiedAt时间戳

**输出**: 无返回值

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child1 = new(SXType.Report, "Report1");
SXObject child2 = new(SXType.Report, "Report2");

root.AddChild("first", child1);
root.AddChild("second", child2);
```

### GetChild(String name)

获取子对象。

**参数**:
- `name`: String - 子对象名称

**输出**: SXObject? - 子对象，不存在返回null

```csharp
SXObject? child = root.GetChild("first");
if (child is not null)
{
    Console.WriteLine($"找到子对象: {child.Name}");
}
```

### RemoveChild(String name)

移除子对象。

**参数**:
- `name`: String - 子对象名称

**内部流程**:
1. 从Children字典移除
2. 清除子对象的Parent引用
3. 更新ModifiedAt时间戳

**输出**: Boolean - 是否成功移除

```csharp
Boolean removed = root.RemoveChild("first");
Console.WriteLine($"移除成功: {removed}");
```

### GetAllChildren()

递归获取所有后代对象。

**输出**: `IEnumerable<SXObject>` - 所有后代对象的枚举

**内部流程**:
1. 遍历直接子对象
2. 递归获取每个子对象的后代
3. 使用yield return延迟返回

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child = new(SXType.Report, "Child");
SXObject grandchild = new(SXType.Report, "Grandchild");

child.AddChild("nested", grandchild);
root.AddChild("sub", child);

Int32 count = root.GetAllChildren().Count();
Console.WriteLine($"后代总数: {count}"); // 2
```

### GetPath()

获取对象路径。

**输出**: String - 从根到当前对象的路径，格式 `parent/child/name`

**内部流程**:
1. 从当前对象开始
2. 向上遍历Parent链
3. 收集所有名称
4. 用"/"连接

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child = new(SXType.Report, "Child");
SXObject grandchild = new(SXType.Report, "Grandchild");

root.AddChild("sub", child);
child.AddChild("nested", grandchild);

Console.WriteLine(grandchild.GetPath());
// 输出: Root/sub/nested
```

### ToString()

返回字符串表示。

**输出**: String - 格式 `[Type] Name`

```csharp
Console.WriteLine(obj.ToString());
// 输出: [Report] AnalysisResult
```

## 使用示例

### 构建分析结果树

```csharp
// 创建根对象
SXObject analysisRoot = new(SXType.Object, "PEAnalysis");

// 设置元数据
analysisRoot.SetMetadata("TargetFile", "sample.exe");
analysisRoot.SetMetadata("AnalysisTime", DateTime.UtcNow.ToString("O"));

// 创建特征子对象
SXObject features = new(SXType.FeatureVector, "Features");
features.SetMetadata("Dimensions", 512);
features.SetMetadata("Sparsity", 0.66);
analysisRoot.AddChild("features", features);

// 创建预测子对象
SXObject prediction = new(SXType.Report, "Prediction");
prediction.SetMetadata("Label", "Benign");
prediction.SetMetadata("Score", -7.70);
prediction.SetMetadata("Probability", 0.0005);
analysisRoot.AddChild("prediction", prediction);

// 创建节表子对象
SXObject sections = new(SXType.Object, "Sections");
sections.SetMetadata("Count", 5);
analysisRoot.AddChild("sections", sections);

// 添加每个节的信息
String[] sectionNames = [".text", ".data", ".rdata", ".rsrc", ".reloc"];
for (Int32 i = 0; i < sectionNames.Length; i++)
{
    SXObject section = new(SXType.Object, $"Section_{i}");
    section.SetMetadata("Name", sectionNames[i]);
    section.SetMetadata("Entropy", 7.2 + i * 0.1);
    sections.AddChild($"section_{i}", section);
}

// 遍历树结构
Console.WriteLine("=== 分析结果树 ===");
PrintTree(analysisRoot, 0);

void PrintTree(SXObject obj, Int32 level)
{
    String indent = new String(' ', level * 2);
    Console.WriteLine($"{indent}[{obj.Type}] {obj.Name ?? obj.ID.ToString("N8")}");
    
    foreach (KeyValuePair<String, SXObject> kvp in obj.Children)
    {
        PrintTree(kvp.Value, level + 1);
    }
}
```

### 查找特定对象

```csharp
// 查找所有报告类型对象
IEnumerable<SXObject> reports = analysisRoot.GetAllChildren()
    .Where(o => o.Type == SXType.Report);

foreach (SXObject report in reports)
{
    Console.WriteLine($"报告: {report.Name}");
    Console.WriteLine($"  标签: {report.GetMetadata<String>("Label")}");
}

// 按路径查找
SXObject? prediction = analysisRoot.GetChild("prediction");
if (prediction is not null)
{
    Double score = prediction.GetMetadata<Double>("Score");
    Console.WriteLine($"预测分数: {score}");
}
```

### 绑定数据

```csharp
// 创建带数据的对象
SXObject dataObj = new(SXType.ByteArray, "RawData");
Byte[] fileBytes = File.ReadAllBytes("sample.exe");
dataObj.Data = new SXData(fileBytes);
dataObj.SetMetadata("Size", fileBytes.Length);

// 后续获取数据
if (dataObj.Data is not null)
{
    Byte[] retrieved = dataObj.Data.GetData();
    Console.WriteLine($"数据大小: {retrieved.Length}");
}
```

## 注意事项

- 树结构通过Parent/Children引用维护
- 添加子对象时自动设置Parent引用
- 移除子对象时自动清除Parent引用
- Metadata支持任意类型值，取值时需类型匹配
- ModifiedAt在修改操作时自动更新
- GetPath()依赖Name属性，若Name为空则使用ID
