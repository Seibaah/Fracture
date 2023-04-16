using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Single;
using MNetNumerics = MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Numerics.LinearAlgebra.Factorization;

public class Tetrahedron : MonoBehaviour
{
    public List<FEM_Vert> meshVerts = new List<FEM_Vert>();
    public Mesh tetMesh;
    public MeshCollider tetCollider;

    //element basis matrix
    public MNetNumerics.Matrix<float> Beta;
    //material reference matrix
    public MNetNumerics.Matrix<float> Du = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //world position matrix
    public MNetNumerics.Matrix<float> Dx = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //velocity matrix
    public MNetNumerics.Matrix<float> Dv = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //deformation gradient matrix
    public MNetNumerics.Matrix<float> F;


    void Start()
    {
        BuildCustomMeshCollider();

        Du.SetColumn(0, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[1].coords - meshVerts[0].coords));
        Du.SetColumn(1, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[2].coords - meshVerts[0].coords));
        Du.SetColumn(2, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[3].coords - meshVerts[0].coords));

        Dx.SetColumn(0, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[1].coords - meshVerts[0].coords)));
        Dx.SetColumn(1, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[2].coords - meshVerts[0].coords)));
        Dx.SetColumn(2, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[3].coords - meshVerts[0].coords)));

        Beta = Du.Inverse();

        F = Dx.Multiply(Beta);

        //TODO perform polar decomposition on F
        var test = F.Svd();
        

    }

    MNetNumerics.Vector<float> ConvertUnityVecToNumerics(Vector3 vec)
    {
        return MNetNumerics.Vector<float>.Build.Dense(new float[] {vec.x, vec.y, vec.z});
    }

    //builds a tetrahderal mesh collider=
    void BuildCustomMeshCollider()
    {
        List<Vector3> vertCoords = new List<Vector3>();
        vertCoords.Add(meshVerts[0].coords);
        vertCoords.Add(meshVerts[1].coords);
        vertCoords.Add(meshVerts[2].coords);
        vertCoords.Add(meshVerts[3].coords);

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

        tetMesh = new Mesh();
        tetMesh.vertices = vertCoords.ToArray();
        tetMesh.SetIndices(new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 }, MeshTopology.Triangles, 0);
        tetMesh.uv = uvs;
        tetMesh.RecalculateNormals();
        tetMesh.RecalculateBounds();


        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = tetMesh;

        tetCollider = gameObject.AddComponent<MeshCollider>();
        tetCollider.sharedMesh = tetMesh;
        tetCollider.convex = true;

        //var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //meshRenderer.material = Resources.Load<Material>("Material_1");
    }

    // Draw the edges of the tetrahedron
    public void Draw()
    {
        List<Vector3> verts = new List<Vector3>();
        tetMesh.GetVertices(verts);
        List<Vector3> w_verts = new List<Vector3>();
        foreach (var v in verts)
        {
            w_verts.Add(transform.TransformPoint(v));
        }

        Debug.DrawLine(w_verts[0], w_verts[1], Color.red);
        Debug.DrawLine(w_verts[0], w_verts[2], Color.red);
        Debug.DrawLine(w_verts[0], w_verts[3], Color.red);
        Debug.DrawLine(w_verts[1], w_verts[2], Color.red);
        Debug.DrawLine(w_verts[1], w_verts[3], Color.red);
        Debug.DrawLine(w_verts[2], w_verts[3], Color.red);        
    }
}