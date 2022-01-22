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

    public static bool LineLineIntersection(out Vector3 intersection,
        Vector3 P1A,
        Vector3 P1B,
        Vector3 P2A,
        Vector3 P2B)
    {
        Vector3 DirP1 = P1B - P1A;
        Vector3 DirP2 = P2B - P2A;

        Vector3 lineVec3 = P2A - P1A;
        Vector3 crossVec1and2 = Vector3.Cross(DirP1, DirP2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, DirP2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Abs(planarFactor) < 0.1f
                && crossVec1and2.sqrMagnitude > 0.1f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                    / crossVec1and2.sqrMagnitude;
            intersection = P1A + (DirP1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    private static bool SameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
    {
        Vector3 cp1 = Vector3.Cross(b - a, p1 - a);
        Vector3 cp2 = Vector3.Cross(b - a, p2 - a);

        if (Vector3.Dot(cp1, cp2) >= 0)
        {
            return true;
        }
        else return false;
    }


    public static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        if (SameSide(p, a, b, c) && SameSide(p, b, a, c))
        {
            return true;
        }
        else return false;
    }

    public static float DistanceLineSegmentPoint(Vector3 a, Vector3 b, Vector3 p)
    {
        // If a == b line segment is a point and will cause a divide by zero in the line segment test.
        // Instead return distance from a
        if (a == b)
            return Vector3.Distance(a, p);

        // Line segment to point distance equation
        Vector3 ba = b - a;
        Vector3 pa = a - p;
        return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
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
    [SerializeField]
    protected float radiusHandle = 0.25f;
    [SerializeField]
    protected Color selectionColorHandle = Color.yellow;
    [SerializeField]
    protected Color colorHandle = Color.white;
    protected Vector3 currentDir = Vector3.zero;

#pragma warning restore 414

    public Vector3 GetPositionAtTime(float _time)
    {
        return GetPositionAtDistance(GetCurrentDistance(_time));
    }
    public Vector3 GetPositionAtDistance(float _dist)
    {
        float dist = Mathf.Clamp(_dist, 0.0f, segments[segments.Length - 1].p2length);

        Vector3 newPos = Vector3.zero;
        Vector3 newDir = Vector3.zero;
        float t = 0.0f;
        for (int i = 0; i < segments.Length; ++i)
        {
            if (dist >= segments[i].p1length && dist <= segments[i].p2length)
            {
                if (segments[i].length == 0)
                {
                    continue;
                }

                t = (dist - segments[i].p1length) / segments[i].length;

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

                    newDir = (newPos - transform.position);

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
    public Vector3 GetPositionAtDistance(float _dist, bool withCatmullRoom)
    {
        float dist = Mathf.Clamp(_dist, 0.0f, segments[segments.Length - 1].p2length);

        Vector3 newPos = Vector3.zero;
        Vector3 newDir = Vector3.zero;
        float t = 0.0f;
        for (int i = 0; i < segments.Length; ++i)
        {
            if (dist >= segments[i].p1length && dist <= segments[i].p2length)
            {
                if (segments[i].length == 0)
                {
                    continue;
                }

                t = (dist - segments[i].p1length) / segments[i].length;

                if (withCatmullRoom)
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

                    newDir = (newPos - transform.position);

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
    public void GetPositionAtTime(Transform transform, out Vector3 position, out Vector3 direction, float _time)
    {
        GetPositionAtDistance(transform, out position, out direction, GetCurrentDistance(_time));
    }
    public void GetPositionAtDistance(Transform transform, out Vector3 position, out Vector3 direction, float _dist)
    {
        float dist = Mathf.Clamp(_dist, 0.0f, segments[segments.Length - 1].p2length);

        position = transform.localPosition;
        direction = transform.forward;

        float t = 0.0f;
        for (int i = 0; i < segments.Length; ++i)
        {
            if (dist >= segments[i].p1length && dist <= segments[i].p2length)
            {
                if (segments[i].length == 0)
                {
                    continue;
                }

                t = (dist - segments[i].p1length) / segments[i].length;

                if (useCatmullRom)
                {
                    if (close)
                    {
                        position = SplineUtils.GetCatmullRomPosition(t,
                        segments[segments.ClampListPos(i - 1)].p1,
                        segments[segments.ClampListPos(i)].p1,
                        segments[segments.ClampListPos(i)].p2,
                        segments[segments.ClampListPos(i + 1)].p2);
                    }
                    else
                    {
                        if (i == 0)
                        {
                            position = SplineUtils.GetCatmullRomPosition(t,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p2,
                            segments[segments.ClampListPos(i + 1)].p2);
                        }
                        else if (i == segments.Length - 1)
                        {
                            position = SplineUtils.GetCatmullRomPosition(t,
                            segments[segments.ClampListPos(i - 1)].p1,
                            segments[segments.ClampListPos(i)].p1,
                            segments[segments.ClampListPos(i)].p2,
                            segments[segments.ClampListPos(i)].p2);
                        }
                        else
                        {
                            position = SplineUtils.GetCatmullRomPosition(t,
                           segments[segments.ClampListPos(i - 1)].p1,
                           segments[segments.ClampListPos(i)].p1,
                           segments[segments.ClampListPos(i)].p2,
                           segments[segments.ClampListPos(i + 1)].p2);
                        }
                    }

                    direction = (position - transform.position);
                }
                else
                {
                    direction = segments[i].dir;
                }
            }
        }
    }
    public float GetCurrentDistance(float _time)
    {
        if (segments != null && segments.Length > 0)
        {
            float totalLength = segments[segments.Length - 1].p2length;
            currentDist = _time * totalLength;
            transform.localPosition = GetPositionAtDistance(currentDist);
            return currentDist;
        }
        else return currentDist;
    }
    public float GetCurrentTime(float _distance)
    {
        float time = 0.0f;
        if (segments != null && segments.Length > 0)
        {
            float totalLength = segments[segments.Length - 1].p2length;
            time = _distance / totalLength;
            return Mathf.Clamp(time, 0.0f, 1.0f);
        }
        else return time;
    }
}
