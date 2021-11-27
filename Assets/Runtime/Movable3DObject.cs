using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Movable3DObject : MonoBehaviour
{
    public enum MovementType
    {
        Linear,
        PingPong
    }

    [System.Serializable]
    public class Segment
    {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 dir;
        public float length = 0.0f;
        public float p1length = 0.0f;
        public float p2length = 0.0f;
    }
#pragma warning disable 414
    [SerializeField]
    private UnityEvent startMovement = null;

    [SerializeField]
    private UnityEvent endMovement = null;

    [SerializeField]
    private MovementType type = MovementType.Linear;
    [SerializeField]
    private Segment[] segments = null;
    [SerializeField]
    private float speed = 5.0f;
    [SerializeField]
    private bool useCurvedSpeed = false;
    [SerializeField]
    private AnimationCurve curve = null;
    [SerializeField]
    private bool isReversed = false;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float startingPos = 0.0f;


    [SerializeField]
    private bool isMovingOnStart = true;
    [SerializeField]
    private bool isChangingDirection = false;
    [SerializeField]
    private bool useCatmullRom = false;
    [SerializeField]
    private bool loop = false;
    [SerializeField]
    private bool close = false;

    [SerializeField]
    private Vector3 center;
    [SerializeField]
    private bool demiCircle = false;

    [SerializeField]
    private float radius = 0.0f;
    [SerializeField]
    private Vector3 rotation = Vector3.zero;
    [SerializeField]
    private float currentDist = 0.0f;
    [SerializeField]
    private bool circleShape = false;

#pragma warning restore 414
    public delegate void OnUpdate();
    public OnUpdate onUpdate;

    private Vector3 currentDir = Vector3.zero;
    private bool pingpong = false;
    private float curvedSpeed = 0.0f;

    public float CurrentDist { get => currentDist; }
    public bool Pingpong { get => pingpong; set => pingpong = value; }
    public Segment[] Segments { get => segments; set => segments = value; }
    public float Speed
    {
        get
        {
            if (useCurvedSpeed)
                return curvedSpeed;
            else
                return speed;
        }
        set => speed = value;
    }
    public Vector3 Center { get => center; set => center = value; }
    public Vector3 CurrentDir
    {
        get
        {
            float sign = 1.0f;
            if (isReversed)
            {
                sign = -1.0f;
            }

            if (pingpong)
            {
                sign *= -1.0f;
            }
            // Debug.Log(currentDir * sign);
            return currentDir * sign;
        }
    }

    private void Start()
    {
        if (isMovingOnStart && startMovement != null)
        {
            startMovement.Invoke();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isMovingOnStart) return;

        switch (type)
        {
            case MovementType.Linear:
                UpdateLinear();
                break;
            case MovementType.PingPong:
                UpdatePingPong();
                break;
        }

        if (isChangingDirection)
            transform.forward = currentDir;
    }

    private void UpdateLinear()
    {
        if (!isReversed)
        {
            UpdatePositionASC(loop);
        }
        else
        {
            UpdatePositionDESC(loop);
        }
    }

    private void UpdatePingPong()
    {
        if (!isReversed)
        {
            if (!pingpong)
            {
                UpdatePositionASC(loop);

                if (currentDist > segments[(segments.Length - 1)].p2length)
                    pingpong = true;
            }
            else
            {
                UpdatePositionDESC(loop);

                if (currentDist < 0.0f)
                    pingpong = false;
            }
        }
        else
        {
            if (pingpong)
            {
                UpdatePositionASC(loop);

                if (currentDist > segments[(segments.Length - 1)].p2length)
                    pingpong = false;
            }
            else
            {
                UpdatePositionDESC(loop);

                if (currentDist < 0.0f)
                    pingpong = true;
            }
        }
    }

    private void UpdateLoop()
    {
        if (!isReversed)
        {
            UpdatePositionASC(true);
        }
        else
        {
            UpdatePositionDESC(true);
        }
    }

    public void Play()
    {
        if (startMovement != null)
        {
            startMovement.Invoke();
        }

        isMovingOnStart = true;
    }

    public void Stop()
    {
        isMovingOnStart = false;
    }

    private Vector3 GetPosition(float _dist)
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
                        newPos = GetCatmullRomPosition(t,
                        segments[ClampListPos(i - 1)].p1,
                        segments[ClampListPos(i)].p1,
                        segments[ClampListPos(i)].p2,
                        segments[ClampListPos(i + 1)].p2);
                    }
                    else
                    {
                        if (i == 0)
                        {
                            newPos = GetCatmullRomPosition(t,
                            segments[ClampListPos(i)].p1,
                            segments[ClampListPos(i)].p1,
                            segments[ClampListPos(i)].p2,
                            segments[ClampListPos(i + 1)].p2);
                        }
                        else if (i == segments.Length - 1)
                        {
                            newPos = GetCatmullRomPosition(t,
                            segments[ClampListPos(i - 1)].p1,
                            segments[ClampListPos(i)].p1,
                            segments[ClampListPos(i)].p2,
                            segments[ClampListPos(i)].p2);
                        }
                        else
                        {
                            newPos = GetCatmullRomPosition(t,
                           segments[ClampListPos(i - 1)].p1,
                           segments[ClampListPos(i)].p1,
                           segments[ClampListPos(i)].p2,
                           segments[ClampListPos(i + 1)].p2);
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



    private void UpdatePositionASC(bool _islooping = false)
    {
        if (currentDist <= segments[segments.Length - 1].p2length)
        {
            if (onUpdate != null)
            {
                onUpdate();
            }

            if (useCurvedSpeed)
            {
                curvedSpeed = curve.Evaluate((currentDist / segments[segments.Length - 1].p2length));
                currentDist += Time.fixedDeltaTime * curvedSpeed;
            }
            else currentDist += Time.fixedDeltaTime * speed;

            if (currentDist > segments[segments.Length - 1].p2length)
            {
                if (_islooping)
                {
                    currentDist = 0.0f;
                }

                if (endMovement != null)
                {
                    endMovement.Invoke();
                }
            }

            transform.localPosition = GetPosition(currentDist);
        }
    }

    private void UpdatePositionDESC(bool _islooping = false)
    {
        if (currentDist >= 0.0f)
        {
            if (onUpdate != null)
            {
                onUpdate();
            }

            if (useCurvedSpeed)
            {
                curvedSpeed = curve.Evaluate(currentDist / segments[segments.Length - 1].p2length);
                currentDist -= Time.fixedDeltaTime * curvedSpeed;
            }
            else currentDist -= Time.fixedDeltaTime * speed;

            if (currentDist < 0.0f)
            {
                if (_islooping)
                {
                    currentDist = segments[segments.Length - 1].p2length;
                }

                if (endMovement != null)
                {
                    endMovement.Invoke();
                }
            }

            transform.localPosition = GetPosition(currentDist);
        }
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

    public Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
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

    public int ClampListPos(int pos)
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
}
