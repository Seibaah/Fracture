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
using UnityEngine.Assertions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;

public class Tetrahedron : MonoBehaviour
{
    //debug
    public bool drawNormals = false;

    public List<FEM_Vert> meshVerts = new List<FEM_Vert>();
    public Mesh tetMesh;
    public MeshCollider tetCollider;

    public int[] vertexOpposedFaces = new int[4];
    public Vector3[] faceNormals = new Vector3[4];

    public float k = 1.9f; //young modulus, in GPa
    public float v = 0.41f; //poisson ratio;
    public float vol; //tet volume

    //[Parker and O'Brien 2009] variables
    //3x3 identity matrix
    public MNetNumerics.Matrix<float> I = MNetNumerics.Matrix<float>.Build.DenseIdentity(3);
    //element basis matrix
    public MNetNumerics.Matrix<float> B;
    //material reference matrix
    public MNetNumerics.Matrix<float> Du = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //world position matrix
    public MNetNumerics.Matrix<float> Dx = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //velocity matrix
    public MNetNumerics.Matrix<float> Dv = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
    //deformation gradient matrix
    public MNetNumerics.Matrix<float> F;

    //OH99 Variables
    //Beta matrix (16)
    public MNetNumerics.Matrix<float> B2 = MNetNumerics.Matrix<float>.Build.Dense(4, 4);
    //X matrix (13)
    public MNetNumerics.Matrix<float> p = MNetNumerics.Matrix<float>.Build.Dense(3, 4);


    void Start()
    {
        BuildCustomMeshCollider();
        ComputeFaceNormals();

        Du.SetColumn(0, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[1].coords - meshVerts[0].coords));
        Du.SetColumn(1, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[2].coords - meshVerts[0].coords));
        Du.SetColumn(2, VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[3].coords - meshVerts[0].coords));

        Dx.SetColumn(0, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[1].coords - meshVerts[0].coords)));
        Dx.SetColumn(1, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[2].coords - meshVerts[0].coords)));
        Dx.SetColumn(2, VectorUtils.ConvertUnityVec3ToNumerics(
            transform.TransformPoint(meshVerts[3].coords - meshVerts[0].coords)));

        B = Du.Inverse();
        F = Dx* B;

        var v0 = meshVerts[0].coords;
        B2.SetColumn(0, MNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v0.x, v0.y, v0.z, 1 }));
        var v1 = meshVerts[1].coords;
        B2.SetColumn(1, MNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v1.x, v1.y, v1.z, 1 }));
        var v2 = meshVerts[2].coords;
        B2.SetColumn(2, MNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v2.x, v2.y, v2.z, 1 }));
        var v3 = meshVerts[3].coords;
        B2.SetColumn(3, MNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v3.x, v3.y, v3.z, 1 }));
        B2 = B2.Inverse();

        var p0 = transform.TransformPoint(v0);
        p.SetColumn(0, VectorUtils.ConvertUnityVec3ToNumerics(p0));
        var p1 = transform.TransformPoint(v1);
        p.SetColumn(1, VectorUtils.ConvertUnityVec3ToNumerics(p1));
        var p2 = transform.TransformPoint(v2);
        p.SetColumn(2, VectorUtils.ConvertUnityVec3ToNumerics(p2));
        var p3 = transform.TransformPoint(v3);
        p.SetColumn(3, VectorUtils.ConvertUnityVec3ToNumerics(p3));

        //polar decomposition on F
        var F_svd = F.Svd();
        var S = F_svd.S;
        var VT = F_svd.VT;
        var V = VT.Transpose();
        var W = F_svd.U;

        //F = U*P = Q*A in Parker, O'Brien
        var P = V * S * VT; //positive definite = V * Σ * VT
        var U = W * VT; //unitary = W * VT
        //remame vars for consistency with the paper
        var Q = U;
        var A = P;

        var Fpow = Q.Transpose() * F; //deformation gradient minus rotation
        var EpsPow = 0.5f * (Fpow + Fpow.Transpose()) - I; //corotational cauchy strain

        //1st and 2nd lamé parameters
        var mu = k / (2 * (1 + v));
        var lambda = (k * v) / ((1 + v) * (1 - 2 * v));

        var s = lambda * EpsPow.Trace() * I + 2 * mu * EpsPow;
        var s_evd = s.Evd();
        var s_eigenvalues = s_evd.EigenValues;
        var s_eigenvectors = s_evd.EigenVectors;

        //compute fi = Q * s * ni for each vert
        for (int i = 0; i < meshVerts.Count(); i++)
        {
            FEM_Vert v = meshVerts[i];
            var ni = faceNormals[i++];
            var Fi = Q * s * VectorUtils.ConvertUnityVec3ToNumerics(ni);
            v.Fi += Fi; //accumulate the force on the vert node
        }

        MNetNumerics.Matrix<float> sPlus = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
        MNetNumerics.Matrix<float> sMin = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < 3; i++)
        {
            sPlus += Mathf.Max(0.0f, ((float)s_eigenvalues.At(i).Magnitude)) 
                * ComputeOperatorM(s_eigenvectors.Column(i));
            sMin += Mathf.Min(0.0f, ((float)s_eigenvalues.At(i).Magnitude))
                * ComputeOperatorM(s_eigenvectors.Column(i));
        }

        //debug
        {
            //var test = s - (sPlus + sMin);
            //if (test.Equals(Matrix<double>.Build.Dense(3, 3, 0.0)))
            //{
            //    Debug.Log("The matrix is only zeros.");
            //}
            //else
            //{
            //    Debug.Log("The matrix is not only zeros.");
            //    Debug.Log(test.ToString());
            //}
        }

        ComputeTetVolume();
        float halfVol = -vol / 2;
        for (int i = 0; i<meshVerts.Count() ; i++)
        {
            FEM_Vert v = meshVerts[i];
            MNetNumerics.Vector<float> forceSum = MNetNumerics.Vector<float>.Build.Dense(3);
            for (int j = 0; j < 4; j++)
            {
                float innerSum = 0;
                for (int k = 0; k < 3; k++)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        innerSum = B2.At(j, l) * B2.At(i, k) * sPlus.At(k, l);
                    }
                }
                forceSum += p.Column(j) * innerSum;
            }
            var FiPlus = halfVol * forceSum;
            v.FiPlus += FiPlus;
            var FiMinus = v.Fi - v.FiPlus;
            v.FiMin += FiMinus;
            v.SetFiPlus.Add(v.FiPlus);
            v.SetFiMin.Add(v.FiMin);
        }
    }

    void Update()
    {
        if (drawNormals) DrawFaceNormals();
    }

    //computes the m operator defined in the Parker and O'Brien paper
    MNetNumerics.Matrix<float> ComputeOperatorM(MNetNumerics.Vector<float> a)
    {
        if(a.At(0) == 0 && a.At(1) == 0 && a.At(2) == 0)
        {
            return MNetNumerics.Matrix<float>.Build.Sparse(3, 3);
        }
        else
        {
            return (a.ToColumnMatrix() * a.ToRowMatrix())/ (float) a.L2Norm();
        }
    }

    //computes the volume of the tetrahedral mesh
    void ComputeTetVolume()
    {
        var a = VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[1].coords - meshVerts[0].coords);
        var b = VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[2].coords - meshVerts[0].coords);
        var c = VectorUtils.ConvertUnityVec3ToNumerics(meshVerts[3].coords - meshVerts[0].coords);
        vol = (1 / 6) * (VectorUtils.CrossProduct(a, b)) * c;
    }

    //computes the face normals
    void ComputeFaceNormals()
    {
        int j = 0;
        for (int i = 0; i < tetMesh.triangles.Length; i += 3)
        {
            var v1 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i]]);
            var v2 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i + 1]]);
            var v3 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i + 2]]);
            var normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            var faceOpposedFEMVertIndex = vertexOpposedFaces[j++];
            faceNormals[faceOpposedFEMVertIndex] = normal;
        }
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

        //vertices opposed to the ordered triangle faces
        vertexOpposedFaces[0] = 3;
        vertexOpposedFaces[1] = 1;
        vertexOpposedFaces[2] = 2;
        vertexOpposedFaces[3] = 0;

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = tetMesh;

        tetCollider = gameObject.AddComponent<MeshCollider>();
        tetCollider.sharedMesh = tetMesh;
        tetCollider.convex = true;

        //var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //meshRenderer.material = Resources.Load<Material>("Material_1");
    }

    //Draw the face normals of the tet
    public void DrawFaceNormals()
    {
        for (int i = 0; i < tetMesh.triangles.Length; i += 3)
        {
            var v1 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i]]);
            var v2 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i + 1]]);
            var v3 = transform.TransformPoint(tetMesh.vertices[tetMesh.triangles[i + 2]]);
            var normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
            var position = (v1 + v2 + v3) / 3f;
            float length = 0.5f * normal.magnitude;
            Debug.DrawRay(position, normal.normalized * length, Color.cyan);
        }
    }

    // Draw the edges of the tetrahedron
    public void DrawMesh()
    {
        List<Vector3> verts = tetMesh.vertices.ToList();
        List<Vector3> wcf_verts = new List<Vector3>();
        foreach (var v in verts)
        {
            wcf_verts.Add(transform.TransformPoint(v));
        }

        Debug.DrawLine(wcf_verts[0], wcf_verts[1], Color.red);
        Debug.DrawLine(wcf_verts[0], wcf_verts[2], Color.red);
        Debug.DrawLine(wcf_verts[0], wcf_verts[3], Color.red);
        Debug.DrawLine(wcf_verts[1], wcf_verts[2], Color.red);
        Debug.DrawLine(wcf_verts[1], wcf_verts[3], Color.red);
        Debug.DrawLine(wcf_verts[2], wcf_verts[3], Color.red);        
    }
}