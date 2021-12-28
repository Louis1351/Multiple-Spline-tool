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
        random
    }

    [SerializeField]
    private GameObject[] spawnableObjects = null;
    [SerializeField]
    private float step = 0.5f;
    [SerializeField]
    private bool useDirection = false;

    [SerializeField]
    private ScaleType scaleType = ScaleType.none;
    [SerializeField]
    private Vector3 scale = Vector3.one;
    [SerializeField]
    private Vector3 minScale = Vector3.one;
    [SerializeField]
    private Vector3 maxScale = Vector3.one;

#pragma warning disable 414
    [SerializeField]
    private bool adaptToSurface = false;
    [SerializeField]
    private float distanceDetection = 0.5f;
    [SerializeField]
    private LayerMask layers = 0;
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
        Vector3 dir = Vector3.zero;
        float t = 0.0f;
        int index = 0;

        ClearObjects();

        do
        {
            go = PrefabUtility.InstantiatePrefab(spawnableObjects[index]) as GameObject;
            go.transform.localPosition = GetPosition(t);

            if (useDirection)
            {
                dir = go.transform.position - GetPosition(t + 0.01f);
                go.transform.forward = dir;
            }

            index++;
            index %= spawnableObjects.Length;

            go.transform.SetParent(transform);
            t += step;

            switch (scaleType)
            {
                case Spawnable3DObject.ScaleType.none:
                    go.transform.localScale = scale;
                    break;
                case Spawnable3DObject.ScaleType.linear:
                    go.transform.localScale = Vector3.Lerp(minScale, maxScale, GetCurrentTime(t));
                    break;
                case Spawnable3DObject.ScaleType.random:
                    // go.transform.localScale = Random.Range();
                    break;
                case Spawnable3DObject.ScaleType.curved:
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
