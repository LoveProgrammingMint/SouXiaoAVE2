# SXTaskID 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXTaskID.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

任务唯一标识符，包含GUID、来源名称、创建时间戳和可选的父任务ID。用于追踪任务执行链和日志关联。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| ID | Guid | 是 | 唯一标识符 |
| CreatedAt | DateTime | 是 | 创建时间（UTC） |
| Source | String | 是 | 来源名称 |
| ParentID | String? | 否 | 父任务ID（可空） |

## 构造函数

### SXTaskID()

无参构造函数，来源设为 "Unknown"。

```csharp
SXTaskID id = new();
Console.WriteLine(id.Source); // "Unknown"
```

### SXTaskID(String source)

指定来源名称。

**参数**:
- `source`: String - 来源标识

```csharp
SXTaskID id = new("LoadPE");
Console.WriteLine(id.Source); // "LoadPE"
```

### SXTaskID(String source, String? parentID)

指定来源和父任务ID。

**参数**:
- `source`: String - 来源标识
- `parentID`: String? - 父任务ID

**内部流程**:
1. 生成新的GUID
2. 记录当前UTC时间
3. 设置来源和父ID

```csharp
SXTaskID parentId = new("Workflow");
SXTaskID childId = new("SubTask", parentId.ID.ToString("N"));
```

## 方法

### ToString()

返回简短字符串表示。

**输出**: String - 格式 `[GUID] Source`

```csharp
SXTaskID id = new("Analyze");
Console.WriteLine(id.ToString());
// 输出: [a1b2c3d4e5f6...] Analyze
```

### ToFullString()

返回完整字符串表示。

**输出**: String - 包含ID、来源、创建时间和父ID

```csharp
SXTaskID id = new("Test", "parent123");
Console.WriteLine(id.ToFullString());
// 输出: ID: a1b2c3d4..., Source: Test, Created: 2026-01-01T12:00:00.0000000Z, Parent: parent123
```

### Parse(String str)

从字符串解析SXTaskID。

**输入**:
- `str`: String - GUID字符串

**输出**: SXTaskID - 解析后的任务ID

**异常**:
- `FormatException`: 无法解析字符串

**内部流程**:
1. 尝试将字符串解析为GUID
2. 成功则创建新SXTaskID，来源设为 "Parsed"

```csharp
String guidStr = "a1b2c3d4e5f67890a1b2c3d4e5f67890";
SXTaskID id = SXTaskID.Parse(guidStr);
Console.WriteLine(id.Source); // "Parsed"
```

### Equals(Object? obj)

比较两个SXTaskID是否相等。

**输入**:
- `obj`: Object? - 要比较的对象

**输出**: Boolean - 是否相等（基于ID比较）

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = SXTaskID.Parse(id1.ID.ToString("N"));
Console.WriteLine(id1.Equals(id2)); // True
```

### GetHashCode()

获取哈希码。

**输出**: Int32 - 基于ID的哈希码

## 运算符

### 相等运算符 (==)

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = SXTaskID.Parse(id1.ID.ToString("N"));
Console.WriteLine(id1 == id2); // True
```

### 不等运算符 (!=)

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = new("Test");
Console.WriteLine(id1 != id2); // True (不同GUID)
```

## 使用示例

### 任务链追踪

```csharp
// 创建主任务ID
SXTaskID mainTaskId = new("MainWorkflow");

// 创建子任务ID
SXTaskID subTask1 = new("LoadFile", mainTaskId.ID.ToString("N"));
SXTaskID subTask2 = new("Analyze", mainTaskId.ID.ToString("N"));

Console.WriteLine($"主任务: {mainTaskId}");
Console.WriteLine($"子任务1: {subTask1.ToFullString()}");
Console.WriteLine($"子任务2: {subTask2.ToFullString()}");
Console.WriteLine($"子任务共享父ID: {subTask1.ParentID == subTask2.ParentID}");
```

### 日志关联

```csharp
void LogTask(SXTaskID taskId, String message)
{
    Console.WriteLine($"[{taskId.ID:N8}] [{taskId.Source}] {message}");
}

SXTaskID id = new("FeatureExtraction");
LogTask(id, "开始提取特征");
LogTask(id, "提取完成，共512维");
```

## 注意事项

- GUID在构造时自动生成，保证全局唯一
- 创建时间使用UTC，避免时区问题
- ParentID为可选，用于构建任务依赖树
- 重写了Equals和GetHashCode，可用于字典键
