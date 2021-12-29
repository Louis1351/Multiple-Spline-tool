using System;
using UnityEngine;
[AttributeUsage(AttributeTargets.Field)]
public class Curve3DAttribute : Attribute
{
    public readonly int RangeX = 0;
    public readonly int RangeY = 0;

    public Curve3DAttribute(int RangeX, int RangeY)
    {
        this.RangeX = RangeX;
        this.RangeY = RangeY;
    }

    public Curve3DAttribute(int RangeX)
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
