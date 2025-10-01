


using UnityEngine;

public interface ITargetable
{
    Transform Transform { get; }
    bool IsValidTarget { get; }
}