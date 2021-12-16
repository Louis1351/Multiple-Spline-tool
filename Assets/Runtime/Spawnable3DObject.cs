using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class Spawnable3DObject : Spline
{

    [SerializeField]
    private GameObject[] spawnableObjects = null;
    [SerializeField]
    private float step = 0.5f;
    [SerializeField]
    private bool useDirection = false;
    
#pragma warning disable 414
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
        Vector3 lastPos = Vector3.zero;
        Vector3 dir = Vector3.zero;
        float t = 0.0f;
        int index = 0;
        bool firstObject = true;
        ClearObjects();

        do
        {
            go = PrefabUtility.InstantiatePrefab(spawnableObjects[index]) as GameObject;
            go.transform.localPosition = GetPosition(t);

            if (useDirection)
            {
                if (close && firstObject)
                {
                    firstObject = false;
                    dir = GetPosition(1.0f) - go.transform.position;
                }
                else dir = lastPos - go.transform.position;
                go.transform.forward = dir;
            }

            index++;
            index %= spawnableObjects.Length;

            go.transform.SetParent(transform);
            t += step;
            lastPos = go.transform.position;
        }

        while (t < segments[segments.Length - 1].p2length);
    }
#endif
}
