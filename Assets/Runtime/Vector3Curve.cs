using System;
using UnityEngine;
[AttributeUsage(AttributeTargets.Field)]
public class Curve3DAttribute : Attribute
{
    public readonly bool RangeX = false;
    public readonly bool RangeY = false;

    public Curve3DAttribute(bool RangeX, bool RangeY)
    {
        this.RangeX = RangeX;
        this.RangeY = RangeY;
    }

    public Curve3DAttribute(bool RangeX)
    {
        this.RangeX = RangeX;
    }
}

[Serializable]
public class Vector3Curve
{
    public Vector2Int range = Vector2Int.one;
    public AnimationCurve curveX;
    public AnimationCurve curveY;
    public AnimationCurve curveZ;
}
