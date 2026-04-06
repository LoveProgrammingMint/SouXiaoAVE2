# SXType 枚举

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXType.cs`  
**类型**: 枚举 (Enum)  
**基类型**: `Byte`

## 概述

定义系统中支持的数据类型枚举，用于标识任务输入输出数据类型和对象类型。

## 枚举值

| 值 | 名称 | 数值 | 描述 |
|---|------|------|------|
| Unknown | 未知类型 | 0 | 默认值，表示未识别的数据类型 |
| PEFile | PE文件 | 1 | Windows可执行文件格式 |
| ByteArray | 字节数组 | 2 | 原始字节数据 |
| FilePath | 文件路径 | 3 | 文件系统路径字符串 |
| Directory | 目录 | 4 | 文件夹路径 |
| Url | 网址 | 5 | URL地址 |
| FeatureVector | 特征向量 | 6 | 机器学习特征向量 |
| Report | 报告 | 7 | 任务执行报告 |
| Task | 任务 | 8 | 任务对象 |
| TaskID | 任务ID | 9 | 任务标识符 |
| Object | 对象 | 10 | 通用对象容器 |

## 使用示例

```csharp
// 创建任务时指定输入输出类型
SXTask task = new("AnalyzePE", SXType.PEFile, SXType.Report);

// 检查数据类型
if (task.InputType == SXType.PEFile)
{
    Console.WriteLine("任务接受PE文件输入");
}

// 创建对象容器
SXObject obj = new(SXType.FeatureVector);
obj.Name = "features_512d";
```

## 注意事项

- 枚举值使用 `Byte` 作为底层类型，节省内存
- `Unknown` 作为默认值，用于错误检测
- 类型检查应在任务执行前进行
