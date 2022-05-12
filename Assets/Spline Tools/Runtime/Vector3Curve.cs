using System;
using UnityEngine;
[AttributeUsage(AttributeTargets.Field)]
public class Curve3DAttribute : Attribute
{
    public readonly bool RangeX = false;
    public readonly bool RangeY = false;
    /// <summary>
    /// Set the x and y range for the curve.
    /// </summary>
    public Curve3DAttribute(bool RangeX, bool RangeY)
    {
        this.RangeX = RangeX;
        this.RangeY = RangeY;
    }
    /// <summary>
    /// Set the x range for the curve.
    /// </summary>
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
