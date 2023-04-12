using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Tetrahedron : MonoBehaviour
{
    public List<Vector3> verts = new List<Vector3>();

    void Start()
    {
        foreach (Vector3 vert in verts)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.position = vert;
            sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            sphere.transform.parent = this.gameObject.transform;
            //sphere.layer = LayerMask.NameToLayer("Geometry");
        }

        // Define the vertices of the tetrahedron
        Vector3[] vertices = new Vector3[] {
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, -1f),
            new Vector3(-1f, 0f, -1f),
            new Vector3(0f, 1f, 0f)
        };
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            uvs[i] = new Vector2(vertex.x, vertex.z);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.SetIndices(new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 }, MeshTopology.Triangles, 0);
        mesh.uv= uvs;
        //mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        //var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //meshRenderer.material = Resources.Load<Material>("Material_1");
    }

    // Draw the edges of the tetrahedron
    public void Draw()
    {
        Debug.DrawLine(verts[0], verts[1], Color.red);
        Debug.DrawLine(verts[0], verts[2], Color.red);
        Debug.DrawLine(verts[0], verts[3], Color.red);
        Debug.DrawLine(verts[1], verts[2], Color.red);
        Debug.DrawLine(verts[1], verts[3], Color.red);
        Debug.DrawLine(verts[2], verts[3], Color.red);        
    }
}