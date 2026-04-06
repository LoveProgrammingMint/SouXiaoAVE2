// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SXAVELinker;

public sealed class SXTaskID
{
    public Guid ID { get; }
    public DateTime CreatedAt { get; }
    public String Source { get; }
    public String? ParentID { get; set; }

    public SXTaskID() : this("Unknown", null)
    {
    }

    public SXTaskID(String source) : this(source, null)
    {
    }

    public SXTaskID(String source, String? parentID)
    {
        ID = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Source = source;
        ParentID = parentID;
    }

    private SXTaskID(Guid id, String source, String? parentID)
    {
        ID = id;
        CreatedAt = DateTime.UtcNow;
        Source = source;
        ParentID = parentID;
    }

    public override String ToString()
    {
        return $"[{ID:N}] {Source}";
    }

    public String ToFullString()
    {
        return $"ID: {ID:N}, Source: {Source}, Created: {CreatedAt:O}, Parent: {ParentID ?? "None"}";
    }

    public static SXTaskID Parse(String str)
    {
        if (Guid.TryParse(str, out Guid id))
        {
            return new SXTaskID(id, "Parsed", null);
        }
        throw new FormatException($"Cannot parse '{str}' as SXTaskID");
    }

    public override Boolean Equals(Object? obj)
    {
        return obj is SXTaskID other && ID.Equals(other.ID);
    }

    public override Int32 GetHashCode()
    {
        return ID.GetHashCode();
    }

    public static Boolean operator ==(SXTaskID? left, SXTaskID? right)
    {
        return left?.ID == right?.ID;
    }

    public static Boolean operator !=(SXTaskID? left, SXTaskID? right)
    {
        return !(left == right);
    }
}
