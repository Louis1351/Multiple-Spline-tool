using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PipeMeshGenerator : Spline
{

#pragma warning restore 414
    [Tooltip("Number of quad for each slice.")]
    [SerializeField]
    private int nbQuad = 4;
    [SerializeField]
    private float width = 2.0f;
    [SerializeField]
    private Material[] materials = null;
    [Tooltip("Set Catmull Rom Precision")]
    [SerializeField]
    private float catmullRomStep = 1.0f;

#pragma warning disable 414
    [Tooltip("Generate the spline each modification.")]
    [SerializeField]
    private bool autoGenerate = false;
#pragma warning restore 414

    int[] quad = new int[]
    {
        0,3,1,
        0,2,3
    };

    public void Generate(Spline.Segment[] segments)
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
        meshRenderer.materials = materials;


        int nbVerticeID = ((nbQuad) * (segments.Length + 1));
        int nbTriangleID = (nbQuad * (segments.Length)) * 6;

        /* Debug.Log(nbVerticeID);
         Debug.Log(nbTriangleID);*/

        ///Set Vertices
        Vector3[] vertices = new Vector3[nbVerticeID + 2];
        int vertID = 0;
        float PI2 = Mathf.PI * 2;
        float div = PI2 / nbQuad;
        Vector3 dir = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        for (int i = 0; i < segments.Length + 1; ++i)
        {

            if (i >= segments.Length)
            {
                rot = Quaternion.LookRotation(segments[i - 1].dir, Vector3.up);
            }
            else
                rot = Quaternion.LookRotation(segments[i].dir, Vector3.up);

            float step = 0;
            for (int j = 0; j < nbQuad; ++j)
            {
                vertices[vertID] = ((i >= segments.Length) ? segments[i - 1].p2 : segments[i].p1) + rot * new Vector3(Mathf.Sin(step), Mathf.Cos(step), 0.0f) * width;
                vertID++;
                step += div;
            }
        }
        vertices[nbVerticeID] = segments[0].p1;
        vertices[nbVerticeID + 1] = segments[segments.Length - 1].p2;

        ///Set triangles
        int[] mainTriangles = new int[nbTriangleID];

        int triID = 0;
        int startF = nbQuad - 2;
        int createQuad = 0;
        for (int p = 0; p < segments.Length; ++p)
        {
            for (int i = 0; i < nbQuad - 1; ++i)
            {
                mainTriangles[triID] = (quad[0] + createQuad);
                triID++;
                mainTriangles[triID] = (quad[1] + createQuad + startF);
                triID++;
                mainTriangles[triID] = (quad[2] + createQuad);
                triID++;

                mainTriangles[triID] = (quad[3] + createQuad);
                triID++;
                mainTriangles[triID] = (quad[4] + createQuad + startF);
                triID++;
                mainTriangles[triID] = (quad[5] + createQuad + startF);
                triID++;
                createQuad++;
            }


            mainTriangles[triID] = (quad[0] + createQuad);
            triID++;
            mainTriangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            mainTriangles[triID] = ((quad[0] + createQuad) - (nbQuad - 1));
            triID++;

            mainTriangles[triID] = (quad[3] + createQuad);
            triID++;
            mainTriangles[triID] = (quad[4] + createQuad + startF);
            triID++;
            mainTriangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            createQuad++;
        }


        nbTriangleID = nbQuad * 6;

        int[] startConnector = new int[nbTriangleID];
        triID = 0;

        for (int i = 0; i < nbQuad; ++i)
        {
            startConnector[triID] = i;
            triID++;
            startConnector[triID] = (i + 1) % nbQuad;
            triID++;
            startConnector[triID] = vertices.Length - 2;
            triID++;
        }

        int[] endConnector = new int[nbTriangleID];
        triID = 0;

        for (int i = (vertices.Length - 2 - nbQuad); i < vertices.Length - 2; ++i)
        {
            endConnector[triID] = vertices.Length - 1;
            triID++;
            endConnector[triID] = (i + 1 >= vertices.Length - 2) ? (vertices.Length - 2 - nbQuad) : (i + 1);
            triID++;
            endConnector[triID] = i;
            triID++;
        }

        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.subMeshCount = 3;

        meshFilter.sharedMesh.name = "pipe";
        meshFilter.sharedMesh.vertices = vertices;

        meshFilter.sharedMesh.SetTriangles(mainTriangles, 0);
        meshFilter.sharedMesh.SetTriangles(startConnector, 1);
        meshFilter.sharedMesh.SetTriangles(endConnector, 2);

        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();
    }

    public void Generate()
    {
        List<Vector3> points = new List<Vector3>();

        if (catmullRomStep <= 0.0f)
            return;

        float t = 0.0f;

        do
        {
            points.Add(GetPositionAtDistance(t));
            t += catmullRomStep;
        }
        while (t < segments[segments.Length - 1].p2length);

        if (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }

        GameObject go = new GameObject("Pipe");
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        go.transform.SetParent(transform);
        meshRenderer.materials = materials;


        int nbVerticeID = ((nbQuad) * (points.Count));
        int nbTriangleID = (nbQuad * (points.Count - 1)) * 6;

        /* Debug.Log(nbVerticeID);
         Debug.Log(nbTriangleID);*/

        ///Set Vertices
        Vector3[] vertices = new Vector3[nbVerticeID + 2];
        int vertID = 0;
        float PI2 = Mathf.PI * 2;
        float div = PI2 / nbQuad;
        Vector3 dir = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        for (int i = 0; i < points.Count; ++i)
        {

            if (i + 1 >= points.Count)
            {
                dir = (points[i] - points[0]);
                if (close)
                    rot = Quaternion.LookRotation(dir, dir);
            }
            else
            {
                dir = (points[i + 1] - points[i]);
                rot = Quaternion.LookRotation(dir, dir);
            }

            float step = 0;
            for (int j = 0; j < nbQuad; ++j)
            {
                vertices[vertID] = points[i] + rot * new Vector3(Mathf.Sin(step), Mathf.Cos(step), 0.0f) * width;
                vertID++;
                step += div;
            }
        }

        vertices[nbVerticeID] = segments[0].p1;
        vertices[nbVerticeID + 1] = points[points.Count - 1];


        ///Set triangles
        int[] mainTriangles = new int[nbTriangleID];

        int triID = 0;
        int startF = nbQuad - 2;
        int createQuad = 0;
        for (int p = 0; p < points.Count - 1; ++p)
        {
            for (int i = 0; i < nbQuad - 1; ++i)
            {
                mainTriangles[triID] = (quad[0] + createQuad);
                triID++;
                mainTriangles[triID] = (quad[1] + createQuad + startF);
                triID++;
                mainTriangles[triID] = (quad[2] + createQuad);
                triID++;

                mainTriangles[triID] = (quad[3] + createQuad);
                triID++;
                mainTriangles[triID] = (quad[4] + createQuad + startF);
                triID++;
                mainTriangles[triID] = (quad[5] + createQuad + startF);
                triID++;
                createQuad++;
            }


            mainTriangles[triID] = (quad[0] + createQuad);
            triID++;
            mainTriangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            mainTriangles[triID] = ((quad[0] + createQuad) - (nbQuad - 1));
            triID++;

            mainTriangles[triID] = (quad[3] + createQuad);
            triID++;
            mainTriangles[triID] = (quad[4] + createQuad + startF);
            triID++;
            mainTriangles[triID] = (quad[0] + createQuad) + 1;
            triID++;
            createQuad++;
        }

        nbTriangleID = nbQuad * 6;

        int[] startConnector = new int[nbTriangleID];
        triID = 0;

        for (int i = 0; i < nbQuad; ++i)
        {
            startConnector[triID] = i;
            triID++;
            startConnector[triID] = (i + 1) % nbQuad;
            triID++;
            startConnector[triID] = vertices.Length - 2;
            triID++;
        }

        int[] endConnector = new int[nbTriangleID];
        triID = 0;

        for (int i = (vertices.Length - 2 - nbQuad); i < vertices.Length - 2; ++i)
        {
            endConnector[triID] = vertices.Length - 1;
            triID++;
            endConnector[triID] = (i + 1 >= vertices.Length - 2) ? (vertices.Length - 2 - nbQuad) : (i + 1);
            triID++;
            endConnector[triID] = i;
            triID++;
        }

        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.subMeshCount = 3;

        meshFilter.sharedMesh.name = "pipe";
        meshFilter.sharedMesh.vertices = vertices;

        meshFilter.sharedMesh.SetTriangles(mainTriangles, 0);
        meshFilter.sharedMesh.SetTriangles(startConnector, 1);
        meshFilter.sharedMesh.SetTriangles(endConnector, 2);

        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();
    }

    public void SaveMesh()
    {
#if UNITY_EDITOR
        MeshFilter meshF = GetComponentInChildren<MeshFilter>();
        if (meshF && meshF.sharedMesh)
        {
            SaveMesh(meshF.sharedMesh);
        }
#endif
    }
#if UNITY_EDITOR
    public static void SaveMesh(Mesh mesh)
    {
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", mesh.name, "asset");

        path = FileUtil.GetProjectRelativePath(path);

        MeshUtility.Optimize(mesh);

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }
#endif
}
