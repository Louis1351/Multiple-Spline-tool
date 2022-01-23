using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class Spawnable3DObject : Spline
{
    [System.Serializable]
    public enum ScaleType
    {
        none,
        linear,
        curved,
        randomBetweenTwoConstant,
        randomByAxis
    }

    [System.Serializable]
    public enum RotationType
    {
        none,
        linear,
        curved,
        randomBetweenTwoConstant,
        randomByAxis
    }
    [Tooltip("Choose the prefabs to Spawn.")]
    [SerializeField]
    private GameObject[] spawnableObjects = null;
    [Tooltip("Space between each object.")]
    [SerializeField]
    private float step = 0.5f;
    [Tooltip("Spawn the objects in the disorder.")]
    [SerializeField]
    private bool randomOrder = false;
    [Tooltip("Spawn each object with a random offset. ")]
    [SerializeField]
    private bool randomOffset = false;
    [Tooltip("Add an offset relative to the spline.")]
    [SerializeField]
    private Vector3Int offsetAxis = Vector3Int.zero;
    [Tooltip("Set the offset distance.")]
    [SerializeField]
    private float distanceOffset = 0.0f;
    [Tooltip("Rotate the spawned object's transform with the spline direction.")]
    [SerializeField]
    private bool useDirection = false;

    [SerializeField]
    private ScaleType scaleType = ScaleType.none;
    [Tooltip("Set the scale for all objects.")]
    [SerializeField]
    private Vector3 currentScale = Vector3.one;
    [Tooltip("Set the minimum scale.")]
    [SerializeField]
    private Vector3 minScale = Vector3.one;
    [Tooltip("Set the maximum scale.")]
    [SerializeField]
    private Vector3 maxScale = Vector3.one;
    [Curve3D(false, true)]
    [SerializeField]
    private Vector3Curve curvedScale;

#pragma warning disable 414
    [SerializeField]
    private RotationType rotationType = RotationType.none;
    [Tooltip("Set the rotation for all objects.")]
    [SerializeField]
    private Vector3 currentRotation = Vector3.zero;
    [Tooltip("Set the minimum rotation.")]
    [SerializeField]
    private Vector3 minRotation = Vector3.zero;
    [Tooltip("Set the maximum rotation.")]
    [SerializeField]
    private Vector3 maxRotation = Vector3.zero;
    [Curve3D(false, true)]
    [SerializeField]
    private Vector3Curve curvedRotation;
    [Tooltip("If it detects a surface, it will change the transform rotation. (only works with collider object)")]
    [SerializeField]
    private bool adaptToSurface = false;
    [Tooltip("Set the distance for the detection.")]
    [SerializeField]
    private float distanceDetection = 0.5f;
    [Tooltip("Choose the layer to detect.")]
    [SerializeField]
    private LayerMask layers = 0;
    [Tooltip("Generate the spline each modification.")]
    [SerializeField]
    private bool autoGenerate = false;
#pragma warning restore 414

    private void ClearObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
#if UNITY_EDITOR
    public void GenerateObjects()
    {
        GameObject go = null;
        Vector3 newScale = Vector3.zero;
        Vector3 newRotation = Vector3.zero;
        Vector3 dir = Vector3.zero;
        Vector3 randomOffsetVec = Vector3.zero;
        float t = 0.0f;
        int index = 0;

        ClearObjects();

        do
        {
            if (randomOffset)
            {
                randomOffsetVec = Random.onUnitSphere * distanceOffset;
                randomOffsetVec.x *= offsetAxis.x;
                randomOffsetVec.y *= offsetAxis.y;
                randomOffsetVec.z *= offsetAxis.z;
            }


            go = PrefabUtility.InstantiatePrefab(spawnableObjects[((randomOrder) ? Random.Range(0, spawnableObjects.Length) : index)]) as GameObject;
            go.transform.localPosition = GetPositionAtDistance(t) + randomOffsetVec;

            dir = go.transform.position - GetPositionAtDistance(t + 0.01f);

            if (useDirection)
            {
                go.transform.forward = dir;
                switch (rotationType)
                {
                    case Spawnable3DObject.RotationType.none:
                        go.transform.localRotation *= Quaternion.Euler(currentRotation);
                        break;
                    case Spawnable3DObject.RotationType.linear:
                        go.transform.localRotation *= Quaternion.Euler(Vector3.Lerp(minRotation, maxRotation, GetCurrentTime(t)));
                        break;
                    case Spawnable3DObject.RotationType.randomBetweenTwoConstant:
                        float l = Random.Range(0f, 1f);
                        go.transform.localRotation *= Quaternion.Euler(Vector3.Lerp(minRotation, maxRotation, l));
                        break;
                    case Spawnable3DObject.RotationType.randomByAxis:
                        newRotation.x = Mathf.Lerp(minRotation.x, maxRotation.x, Random.Range(0f, 1f));
                        newRotation.y = Mathf.Lerp(minRotation.y, maxRotation.y, Random.Range(0f, 1f));
                        newRotation.z = Mathf.Lerp(minRotation.z, maxRotation.z, Random.Range(0f, 1f));

                        go.transform.localRotation *= Quaternion.Euler(newRotation);
                        break;
                    case Spawnable3DObject.RotationType.curved:
                        float currentT = GetCurrentTime(t);
                        newRotation.x = curvedRotation.curveX.Evaluate(currentT);
                        newRotation.y = curvedRotation.curveY.Evaluate(currentT);
                        newRotation.z = curvedRotation.curveZ.Evaluate(currentT);

                        go.transform.localRotation *= Quaternion.Euler(newRotation);
                        break;
                }
            }
            else
            {
                switch (rotationType)
                {
                    case Spawnable3DObject.RotationType.none:
                        go.transform.localRotation = Quaternion.Euler(currentRotation);
                        break;
                    case Spawnable3DObject.RotationType.linear:
                        go.transform.localRotation = Quaternion.Euler(Vector3.Lerp(minRotation, maxRotation, GetCurrentTime(t)));
                        break;
                    case Spawnable3DObject.RotationType.randomBetweenTwoConstant:
                        float l = Random.Range(0f, 1f);
                        go.transform.localRotation = Quaternion.Euler(Vector3.Lerp(minRotation, maxRotation, l));
                        break;
                    case Spawnable3DObject.RotationType.randomByAxis:
                        newRotation.x = Mathf.Lerp(minRotation.x, maxRotation.x, Random.Range(0f, 1f));
                        newRotation.y = Mathf.Lerp(minRotation.y, maxRotation.y, Random.Range(0f, 1f));
                        newRotation.z = Mathf.Lerp(minRotation.z, maxRotation.z, Random.Range(0f, 1f));

                        go.transform.localRotation = Quaternion.Euler(newRotation);
                        break;
                    case Spawnable3DObject.RotationType.curved:
                        float currentT = GetCurrentTime(t);
                        newRotation.x = curvedRotation.curveX.Evaluate(currentT);
                        newRotation.y = curvedRotation.curveY.Evaluate(currentT);
                        newRotation.z = curvedRotation.curveZ.Evaluate(currentT);

                        go.transform.localRotation = Quaternion.Euler(newRotation);
                        break;
                }
            }

            index++;
            index %= spawnableObjects.Length;

            go.transform.SetParent(transform);
            t += step;

            switch (scaleType)
            {
                case Spawnable3DObject.ScaleType.none:
                    go.transform.localScale = currentScale;
                    break;
                case Spawnable3DObject.ScaleType.linear:
                    go.transform.localScale = Vector3.Lerp(minScale, maxScale, GetCurrentTime(t));
                    break;
                case Spawnable3DObject.ScaleType.randomBetweenTwoConstant:
                    float l = Random.Range(0f, 1f);
                    go.transform.localScale = Vector3.Lerp(minScale, maxScale, l);
                    break;
                case Spawnable3DObject.ScaleType.randomByAxis:
                    newScale.x = Mathf.Lerp(minScale.x, maxScale.x, Random.Range(0f, 1f));
                    newScale.y = Mathf.Lerp(minScale.y, maxScale.y, Random.Range(0f, 1f));
                    newScale.z = Mathf.Lerp(minScale.z, maxScale.z, Random.Range(0f, 1f));

                    go.transform.localScale = newScale;
                    break;
                case Spawnable3DObject.ScaleType.curved:
                    float currentT = GetCurrentTime(t);
                    newScale.x = curvedScale.curveX.Evaluate(currentT);
                    newScale.y = curvedScale.curveY.Evaluate(currentT);
                    newScale.z = curvedScale.curveZ.Evaluate(currentT);

                    go.transform.localScale = newScale;

                    break;
            }

            if (adaptToSurface)
            {
                Collider[] colliders = Physics.OverlapSphere(go.transform.position, distanceDetection, layers);
                foreach (Collider collider in colliders)
                {
                    if (collider.transform.parent != transform)
                    {
                        Vector3 nearestObjPoint = collider.ClosestPoint(go.transform.position);
                        Vector3 direction = (nearestObjPoint - go.transform.position).normalized;

                        if (Physics.Raycast(go.transform.position, direction, out RaycastHit hit1, Mathf.Infinity, layers)
                        && Physics.Raycast(go.transform.position + dir, direction, out RaycastHit hit2, Mathf.Infinity, layers))
                        {
                            Collider c = go.GetComponent<Collider>();
                            if (c)
                            {
                                go.transform.position = hit1.point + go.transform.localScale.y * c.bounds.size.y * 0.5f * hit1.normal;
                                go.transform.rotation = Quaternion.LookRotation(hit2.point - hit1.point, hit1.normal);
                            }
                        }
                        break;
                    }
                }
            }


        }

        while (t < segments[segments.Length - 1].p2length);
    }
#endif
}
