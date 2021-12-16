using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SplineUtils
{
    public static int ClampListPos(this Spline.Segment[] segments, int pos)
    {
        if (pos < 0)
        {
            pos = segments.Length - 1;
        }
        if (pos > segments.Length - 1)
        {
            pos = 0;
        }

        return pos;
    }

    public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        //The cubic polynomial: a + b * t + c * t^2 + d * t^3
        Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

        return pos;
    }
}

public class Spline : MonoBehaviour
{
    [System.Serializable]
    public class Segment
    {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 dir;
        public float length = 0.0f;
        public float p1length = 0.0f;
        public float p2length = 0.0f;

        public Segment()
        {
        }

        public Segment(Segment segment)
        {
            this.p1 = segment.p1;
            this.p2 = segment.p2;
            this.dir = segment.dir;
            this.length = segment.length;
            this.p1length = segment.p1length;
            this.p2length = segment.p2length;
        }
    }

#pragma warning disable 414
    [SerializeField]
    public Segment[] segments = null;
    [SerializeField]
    protected bool useCatmullRom = false;
    [SerializeField]
    protected bool close = false;
    [SerializeField]
    public Vector3 center;
    [SerializeField]
    protected bool demiCircle = false;

    [SerializeField]
    protected float radius = 0.0f;
    [SerializeField]
    protected Vector3 rotation = Vector3.zero;
    [SerializeField]
    protected float currentDist = 0.0f;
    [SerializeField]
    protected bool circleShape = false;
    protected Vector3 currentDir = Vector3.zero;

#pragma warning restore 414

    public Vector3 GetPosition(float _dist)
    {
        float dist = Mathf.Clamp(_dist, 0.0f, segments[segments.Length - 1].p2length);

        Vector3 newPos = Vector3.zero;
        for (int i = 0; i < segments.Length; ++i)
        {
            if (dist >= segments[i].p1length && dist <= segments[i].p2length)
            {
                if (segments[i].length == 0)
                {
                    continue;
                }

                float t = (dist - segments[i].p1length) / segments[i].length;

                if (useCatmullRom)
                {
                    if (close)
                    {
                        newPos = SplineUtils.GetCatmullRomPosition(t,
                        segments[segments.ClampListPos(i - 1)].p1,
                        segments[segments.ClampListPos(i)].p1,
                        segments[segments.ClampListPos(i)].p2,
                        segments[segments.ClampListPos(i + 1)].p2);
                    }
                    else
                    {
                        if (i == 0)
                        {
                            newPos = SplineUtils.GetCatmullRomPosition(t,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p2,
                            segments[segments.ClampListPos(i + 1)].p2);
                        }
                        else if (i == segments.Length - 1)
                        {
                            newPos = SplineUtils.GetCatmullRomPosition(t,
                            segments[segments.ClampListPos(i - 1)].p1,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p2,
                            segments[segments.ClampListPos(i)].p2);
                        }
                        else
                        {
                            newPos = SplineUtils.GetCatmullRomPosition(t,
                           segments[segments.ClampListPos(i - 1)].p1,
                           segments[segments.ClampListPos(i)].p1,
                           segments[segments.ClampListPos(i)].p2,
                           segments[segments.ClampListPos(i + 1)].p2);
                        }
                    }
                    Vector3 newDir = (newPos - transform.position).normalized;
                    if (newDir != Vector3.zero)
                        currentDir = newDir;
                    return newPos;
                }
                else
                {
                    currentDir = segments[i].dir;
                    return Vector3.Lerp(segments[i].p1, segments[i].p2, t);
                }
            }
        }
        return transform.localPosition;
    }

    public float GetCurrentDistance(float _time)
    {
        if (segments != null && segments.Length > 0)
        {
            float totalLength = segments[segments.Length - 1].p2length;
            currentDist = _time * totalLength;
            transform.localPosition = GetPosition(currentDist);
            return currentDist;
        }
        else return currentDist;
    }
}
