using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Movable3DObject : Spline
{
    public enum MovementType
    {
        Linear,
        PingPong
    }

    public delegate void OnUpdate();
    public OnUpdate onUpdate;
    [SerializeField]
    private UnityEvent startMovement = null;

    [SerializeField]
    private UnityEvent endMovement = null;

    [SerializeField]
    private MovementType type = MovementType.Linear;
    [SerializeField]
    private float speed = 5.0f;
    [SerializeField]
    private bool useCurvedSpeed = false;
    [SerializeField]
    private AnimationCurve curve = null;
    [SerializeField]
    private bool isReversed = false;
#pragma warning disable 414
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float startingPos = 0.0f;
#pragma warning restore 414
    [SerializeField]
    private bool isMovingOnStart = true;
    [SerializeField]
    private bool isChangingDirection = false;
    [SerializeField]
    private bool loop = false;
    private bool pingpong = false;
    private float curvedSpeed = 0.0f;

    public float CurrentDist { get => currentDist; }
    public bool Pingpong { get => pingpong; set => pingpong = value; }

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

            transform.localPosition = GetPositionAtDistance(currentDist);
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

            transform.localPosition = GetPositionAtDistance(currentDist);
        }
    }

}
