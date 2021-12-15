using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeMeshGenerator : MonoBehaviour
{
    [SerializeField]
    public Segment[] segments = null;

    [SerializeField]
    private bool useCatmullRom = false;
    [SerializeField]
    private bool close = false;

    [SerializeField]
    public Vector3 center;
    [SerializeField]
    private bool demiCircle = false;

    [SerializeField]
    private float radius = 0.0f;
    [SerializeField]
    private Vector3 rotation;
    [SerializeField]
    private float currentDist = 0.0f;

#pragma warning restore 414
    private Vector3 currentDir = Vector3.zero;
    [SerializeField]
    private bool circleShape = false;

    [SerializeField]
    private int nbQuad = 4;
    [SerializeField]
    private float width = 2.0f;

    [SerializeField]
    private Material material = null;

    public float CurrentDist { get => currentDist; }

    public Segment[] Segments { get => segments; set => segments = value; }

    public Vector3 Center { get => center; set => center = value; }
    int[] quad = new int[]
      /* {
            0,3,2,
            0,1,3
            };*/
            {
                0,3,1,
                0,2,3
            };

    public void Generate()
    {
        if (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }

        GameObject go = new GameObject("Pipe");
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        go.transform.SetParent(transform);
        meshRenderer.material = material;


        //  Vector3 center = transform.position;
        /* int nbVerticeID = (nbQuad) * 2;

         int nbTriangleID = nbVerticeID * 3;

         go.transform.SetParent(transform);

         meshRenderer.material = material;

         Vector3[] vertices = new Vector3[nbVerticeID];
         int[] triangles = new int[nbVerticeID];
         int addVertByQuad = 0;

         float PI2 = Mathf.PI * 2;
         float div = PI2 / nbQuad;
         float step = 0;*/

        /*  for (int vertID = 0; vertID < vertices.Length; vertID += 2)
          {
              vertices[vertID] = center + new Vector3(Mathf.Sin(step), Mathf.Cos(step), -0.5f) * width;
              vertices[vertID + 1] = center + new Vector3(Mathf.Sin(step), Mathf.Cos(step), 0.5f) * width;
              step += div;
          }

          int triID = 0;
          for (int i = 0; i < nbQuad; i++)
          {
              addVertByQuad = 2 * i;

              for (int quadID = 0; quadID < quad.Length; quadID++)
              {
                  triangles[triID] = (quad[quadID] + addVertByQuad) % nbVerticeID;
                  triID++;
              }
          }*/

        int nbVerticeID = ((nbQuad) * segments.Length);
        int nbTriangleID = (nbQuad * (segments.Length - 1)) * 6;

        Debug.Log(nbVerticeID);
        Debug.Log(nbTriangleID);

        ///Set Vertices
        Vector3[] vertices = new Vector3[nbVerticeID];
        int vertID = 0;
        float PI2 = Mathf.PI * 2;
        float div = PI2 / nbQuad;
        Vector3 dir = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        for (int i = 0; i < segments.Length; ++i)
        {

            rot = Quaternion.LookRotation(segments[i].dir, Vector3.up);

            float step = 0;
            for (int j = 0; j < nbQuad; ++j)
            {
                vertices[vertID] = segments[i].p1 + rot * new Vector3(Mathf.Sin(step), Mathf.Cos(step), 0.0f) * width;
                vertID++;
                step += div;
            }
        }


        ///Set triangles
        int[] triangles = new int[nbTriangleID];

        int triID = 0;
        int startF = nbQuad - 2;
        int createQuad = 0;
        for (int p = 0; p < segments.Length - 1; ++p)
        {
            for (int i = 0; i < nbQuad - 1; ++i)
            {
                triangles[triID] = (quad[0] + createQuad);
                triID++;
                triangles[triID] = (quad[1] + createQuad + startF);
                triID++;
                triangles[triID] = (quad[2] + createQuad);
                triID++;

                triangles[triID] = (quad[3] + createQuad);
                triID++;
                triangles[triID] = (quad[4] + createQuad + startF);
                triID++;
                triangles[triID] = (quad[5] + createQuad + startF);
                triID++;
                createQuad++;
            }


            triangles[triID] = (quad[0] + createQuad);
            triID++;
            triangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            triangles[triID] = ((quad[0] + createQuad) - (nbQuad - 1));
            triID++;

            triangles[triID] = (quad[3] + createQuad);
            triID++;
            triangles[triID] = (quad[4] + createQuad + startF);
            triID++;
            triangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            createQuad++;
        }


        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.name = "pipe";
        meshFilter.sharedMesh.vertices = vertices;
        meshFilter.sharedMesh.triangles = triangles;

        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();
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
