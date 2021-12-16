using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeMeshGenerator : Spline
{

#pragma warning restore 414

    [SerializeField]
    private int nbQuad = 4;
    [SerializeField]
    private float width = 2.0f;

    [SerializeField]
    private Material material = null;

#pragma warning disable 414
    [SerializeField]
    private bool autoGenerate = false;
#pragma warning restore 414

    int[] quad = new int[]
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

        int nbVerticeID = ((nbQuad) * segments.Length);
        int nbTriangleID = (nbQuad * (segments.Length - 1)) * 6;

        /* Debug.Log(nbVerticeID);
         Debug.Log(nbTriangleID);*/

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
}
