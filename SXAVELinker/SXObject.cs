// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SXAVELinker;

public sealed class SXObject
{
    public Guid ID { get; }
    public SXType Type { get; }
    public String? Name { get; set; }
    public SXData? Data { get; set; }
    public Dictionary<String, Object> Metadata { get; }
    public Dictionary<String, SXObject> Children { get; }
    public SXObject? Parent { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; private set; }

    public SXObject(SXType type)
    {
        ID = Guid.NewGuid();
        Type = type;
        Metadata = new Dictionary<String, Object>();
        Children = new Dictionary<String, SXObject>();
        CreatedAt = DateTime.UtcNow;
    }

    public SXObject(SXType type, String name) : this(type)
    {
        Name = name;
    }

    public void SetMetadata(String key, Object value)
    {
        Metadata[key] = value;
        ModifiedAt = DateTime.UtcNow;
    }

    public T? GetMetadata<T>(String key)
    {
        if (Metadata.TryGetValue(key, out Object? value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddChild(String name, SXObject child)
    {
        child.Parent = this;
        Children[name] = child;
        ModifiedAt = DateTime.UtcNow;
    }

    public SXObject? GetChild(String name)
    {
        return Children.TryGetValue(name, out SXObject? child) ? child : null;
    }

    public Boolean RemoveChild(String name)
    {
        if (Children.Remove(name, out SXObject? child))
        {
            child.Parent = null;
            ModifiedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public IEnumerable<SXObject> GetAllChildren()
    {
        foreach (KeyValuePair<String, SXObject> kvp in Children)
        {
            yield return kvp.Value;
            foreach (SXObject descendant in kvp.Value.GetAllChildren())
            {
                yield return descendant;
            }
        }
    }

    public String GetPath()
    {
        List<String> parts = [Name ?? ID.ToString("N")];
        SXObject? current = Parent;
        while (current is not null)
        {
            parts.Insert(0, current.Name ?? current.ID.ToString("N"));
            current = current.Parent;
        }
        return String.Join("/", parts);
    }

    public override String ToString()
    {
        return $"[{Type}] {Name ?? ID.ToString("N8")}";
    }
}
