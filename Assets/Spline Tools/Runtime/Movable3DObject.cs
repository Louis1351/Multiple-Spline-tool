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
    private Transform target = null;
    [Tooltip("Execute an event when the object starts moving at the beginning of the spline.")]
    [SerializeField]
    private UnityEvent startMovement = null;
    [Tooltip("Execute an event when the object arrives at the end of the spline.")]
    [SerializeField]
    private UnityEvent endMovement = null;
    [Tooltip("Change the movement type.")]
    [SerializeField]
    private MovementType type = MovementType.Linear;
    [Tooltip("the object's speed on the spline.")]
    [SerializeField]
    private float speed = 5.0f;
    [SerializeField]
    private bool useCurvedSpeed = false;
    [Tooltip("the object's speed on the spline multiplies by the curve.")]
    [SerializeField]
    private AnimationCurve curve = null;
    [Tooltip("Reverse the speed.")]
    [SerializeField]
    private bool isReversed = false;
#pragma warning disable 414
    [Tooltip("The starting position on the spline.")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float startingPos = 0.0f;
#pragma warning restore 414
    [SerializeField]
    private bool isMovingOnStart = true;
    [Tooltip("Rotate the object's transform with the spline direction.")]
    [SerializeField]
    private bool isChangingDirection = false;
    [Tooltip("When the object arrives at the end of the spline, it goes back to the beginning.")]
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

    /* public Vector3 CurrentDir
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
     }*/

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
        if (target && currentDist <= segments[segments.Length - 1].p2length)
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

            GetPositionAtDistance(target, out Vector3 position, out Vector3 dir, out float angle, currentDist);

            target.localPosition = position;

            if (isChangingDirection && dir != Vector3.zero)
            {
                target.forward = dir;
                target.Rotate(dir, angle, Space.World);
            }
        }
    }

    private void UpdatePositionDESC(bool _islooping = false)
    {
        if (target && currentDist >= 0.0f)
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

            GetPositionAtDistance(target, out Vector3 position, out Vector3 dir, out float angle, currentDist);

            target.localPosition = position;

            if (isChangingDirection && dir != Vector3.zero)
            {
                target.forward = dir;
                target.Rotate(dir, angle, Space.World);
            }
        }
    }

}
