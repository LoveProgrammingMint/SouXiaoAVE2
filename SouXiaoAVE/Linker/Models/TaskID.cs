// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;

namespace SouXiaoAVE.Linker.Models;

public sealed class TaskID : IEquatable<TaskID>
{
    public Guid Id { get; init; }

    public DateTime CreatedTime { get; init; }

    public TaskID()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public TaskID(Guid id)
    {
        Id = id;
        CreatedTime = DateTime.UtcNow;
    }

    public static TaskID NewTaskID() => new();

    public static TaskID Empty => new(Guid.Empty);

    public Boolean IsEmpty => Id == Guid.Empty;

    public override String ToString() => Id.ToString("N");

    public Boolean Equals(TaskID? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id;
    }

    public override Boolean Equals(Object? obj) => Equals(obj as TaskID);

    public override Int32 GetHashCode() => Id.GetHashCode();

    public static Boolean operator ==(TaskID? left, TaskID? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static Boolean operator !=(TaskID? left, TaskID? right) => !(left == right);
}
