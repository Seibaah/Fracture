//#define DEBUG_MODE_ON

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

public class Tetrahedron : MonoBehaviour
{
#if DEBUG_MODE_ON
    [Header("Debug")]
    public bool drawNormals = false;
#endif

    [Header("FEM Elements")]
    public FemMesh parentFemMesh;
    public Rigidbody parentFemRb;

    [Header("Tetrahedron")]
    public Vector3 centroid;
    public Mesh tetMesh;
    public MeshCollider tetCollider;
    public List<FemVert> meshVerts = new List<FemVert>();
    float vol;
    int[] vertexOpposedFaces = new int[4]; 
    Vector3[] faceNormals = new Vector3[4];

    [Header("Material Parameters")]
    public float k = 1.9f; //young modulus, in GPa
    public float v = 0.41f; //poisson ratio;

    /// <summary>
    /// Variables defined in Parker, G., & O'Brien, J. F. (2009). 
    /// Real-time deformation and fracture in a game environment. 
    /// In ACM SIGGRAPH 2009 Talks (p. 18). ACM.
    /// </summary>
    readonly MathNetNumerics.Matrix<float> I = MathNetNumerics.Matrix<float>.Build.DenseIdentity(3); //3x3 identity matrix
    MathNetNumerics.Matrix<float> B; //element basis matrix Beta (1)
    MathNetNumerics.Matrix<float> Du = MathNetNumerics.Matrix<float>.Build.Dense(3, 3); //material reference matrix
    MathNetNumerics.Matrix<float> Dx = MathNetNumerics.Matrix<float>.Build.Dense(3, 3); //world position matrix
    MathNetNumerics.Matrix<float> F; //deformation gradient matrix (1)

    /// <summary>
    /// Variables defined in O'Brien, J. F., & Hodgins, J. K. (1999). 
    /// Graphical modeling and animation of brittle fracture. 
    /// Proceedings of the 26th annual conference on Computer graphics and interactive techniques, 27-34. ACM.
    /// </summary>
    MathNetNumerics.Matrix<float> B2 = MathNetNumerics.Matrix<float>.Build.Dense(4, 4); //Beta matrix (16)
    MathNetNumerics.Matrix<float> X = MathNetNumerics.Matrix<float>.Build.Dense(3, 4); //X matrix (13)


    void Start()
    {
        BuildTetrahedronMeshCollider();
        ComputeFaceNormals();
        ComputeCentroid();
        ComputeVolume();
    }

    void Update()
    {
#if DEBUG_MODE_ON
        if (drawNormals) DrawFaceNormals();
#endif

        if (parentFemRb == null)
        {
            parentFemRb = parentFemMesh.gameObject.GetComponent<Rigidbody>();
        }

        //run fracture algorithm is parent FEM mesh enables it
        if (parentFemMesh.computeFracture) 
        {
            ComputeFaceNormals();
            ComputeCentroid();
            ComputeFracture();
        }
    }

    /// <summary>
    /// Computes and assigns tensile and compressive forces acting on each tetrahedron vertex.
    /// Based on the work described in O'Brien, J. F., & Hodgins, J. K. (1999) and
    /// Parker, G., & O'Brien, J. F. (2009)
    /// </summary>
    void ComputeFracture()
    {
        //Finite Element Formulation steps. Parker, G., & O'Brien, J. F. (2009)
        Du.SetColumn(0, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[1].pos - meshVerts[0].pos));
        Du.SetColumn(1, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[2].pos - meshVerts[0].pos));
        Du.SetColumn(2, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[3].pos - meshVerts[0].pos));

        Dx.SetColumn(0, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(
            transform.TransformPoint(meshVerts[1].pos - meshVerts[0].pos)));
        Dx.SetColumn(1, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(
            transform.TransformPoint(meshVerts[2].pos - meshVerts[0].pos)));
        Dx.SetColumn(2, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(
            transform.TransformPoint(meshVerts[3].pos - meshVerts[0].pos)));

        B = Du.Inverse();
        F = Dx * B;

        //Finite Element Discretization. O'Brien, J. F., & Hodgins, J. K. (1999)
        var v0 = meshVerts[0].pos;
        B2.SetColumn(0, MathNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v0.x, v0.y, v0.z, 1 }));
        var v1 = meshVerts[1].pos;
        B2.SetColumn(1, MathNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v1.x, v1.y, v1.z, 1 }));
        var v2 = meshVerts[2].pos;
        B2.SetColumn(2, MathNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v2.x, v2.y, v2.z, 1 }));
        var v3 = meshVerts[3].pos;
        B2.SetColumn(3, MathNetNumerics.Vector<float>.Build.DenseOfArray(new float[] { v3.x, v3.y, v3.z, 1 }));
        B2 = B2.Inverse();

        var p0 = transform.TransformPoint(v0);
        X.SetColumn(0, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(p0));
        var p1 = transform.TransformPoint(v1);
        X.SetColumn(1, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(p1));
        var p2 = transform.TransformPoint(v2);
        X.SetColumn(2, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(p2));
        var p3 = transform.TransformPoint(v3);
        X.SetColumn(3, VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(p3));

        //Compute Polar Decomposition of the Deformation Gradient F using its Single Value Decomposition.
        //F. Parker, G., & O'Brien, J. F. (2009)
        var F_svd = F.Svd();
        var S = F_svd.S; //S -> Σ in most literature
        var VT = F_svd.VT;
        var V = VT.Transpose();
        var W = F_svd.U;
        //F = U*P = Q*A
        var P = V * S * VT; //positive definite matrix = V * Σ * VT
        var U = W * VT; //unitary matrix = W * VT
        //remame vars for consistency with the paper
        var Q = U;
        var A = P;

        //Factoring out rotational effect from deformation gradient. F. Parker, G., & O'Brien, J. F. (2009)
        var Fpow = Q.Transpose() * F;
        var EpsPow = 0.5f * (Fpow + Fpow.Transpose()) - I; //corotational cauchy strain

        //Compute 1st and 2nd lamé parameters. F. Parker, G., & O'Brien, J. F. (2009)
        var mu = k / (2 * (1 + v));
        var lambda = (k * v) / ((1 + v) * (1 - 2 * v));

        //Compute element stress. F. Parker, G., & O'Brien, J. F. (2009)
        var s = lambda * EpsPow.Trace() * I + 2 * mu * EpsPow;
        var s_evd = s.Evd();
        var s_eigenvalues = s_evd.EigenValues;
        var s_eigenvectors = s_evd.EigenVectors;

        //Reset all forces on each vertex element
        foreach (FemVert v in meshVerts)
        { 
            v.Fi.Clear();
            v.FiPlus.Clear();
            v.FiMin.Clear();
            v.SetFiPlus.Clear();
            v.SetFiMin.Clear();
        }
        //Compute fi = Q * s * ni for each vertex element. F. Parker, G., & O'Brien, J. F. (2009)
        for (int i = 0; i < meshVerts.Count(); i++)
        {
            FemVert v = meshVerts[i];
            var ni = faceNormals[i++];
            var Fi = Q * s * VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(ni);
            v.Fi += Fi; //accumulate the force on the vert node
        }

        //Compute Force Decomposition. O'Brien, J. F., & Hodgins, J. K. (1999)
        MathNetNumerics.Matrix<float> sPlus = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);
        MathNetNumerics.Matrix<float> sMin = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < 3; i++)
        {
            sPlus += Mathf.Max(0.0f, ((float)s_eigenvalues.At(i).Magnitude))
                * ComputeOperatorM(s_eigenvectors.Column(i));
            sMin += Mathf.Min(0.0f, ((float)s_eigenvalues.At(i).Magnitude))
                * ComputeOperatorM(s_eigenvectors.Column(i));
        }

#if DEBUG_MODE_ON       
        var test = s - (sPlus + sMin);
        if (test.Equals(Matrix<double>.Build.Dense(3, 3, 0.0)))
        {
            Debug.Log("The matrix is only zeros.");
        }
        else
        {
            //in practice test matrix isn't 0 most of the time due to rounding errors
            //values are in in E-08 or smaller so it's acceptable
            Debug.Log("The matrix is not only zeros."); 
            Debug.Log(test.ToString());
        }
#endif

        //Compute tensile and compressive forces acting on each vertex. O'Brien, J. F., & Hodgins, J. K. (1999)
        float halfVol = -vol / 2;
        for (int i = 0; i < meshVerts.Count(); i++)
        {
            FemVert v = meshVerts[i];
            MathNetNumerics.Vector<float> forceSum = MathNetNumerics.Vector<float>.Build.Dense(3);
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
                forceSum += X.Column(j) * innerSum;
            }
            var FiPlus = halfVol * forceSum;
            v.FiPlus += FiPlus;
            var FiMinus = v.Fi - v.FiPlus;
            v.FiMin += FiMinus;
            v.SetFiPlus.Add(v.FiPlus);
            v.SetFiMin.Add(v.FiMin);
        }
    }

    /// <summary>
    /// Computes the m(A) matrix operator defined in O'Brien, J. F., & Hodgins, J. K. (1999). 
    /// Graphical modeling and animation of brittle fracture. 
    /// Proceedings of the 26th annual conference on Computer graphics and interactive techniques, 27-34. ACM.
    /// </summary>
    /// <param name="a">Vector in R3</param>
    /// <returns>3x3 symmetric matrix that has magnitude(a) as its only non-zero eigenvalue</returns>
    MathNetNumerics.Matrix<float> ComputeOperatorM(MathNetNumerics.Vector<float> a)
    {
        if(a.At(0) == 0 && a.At(1) == 0 && a.At(2) == 0)
        {
            return MathNetNumerics.Matrix<float>.Build.Sparse(3, 3);
        }
        else
        {
            return (a.ToColumnMatrix() * a.ToRowMatrix()) / (float) a.L2Norm();
        }
    }

    /// <summary>
    /// Creates a tetrahedron Mesh Collider
    /// </summary>
    void BuildTetrahedronMeshCollider()
    {
        // Define the vertices of the tetrahedron
        List<Vector3> vertCoords = new List<Vector3>();
        vertCoords.Add(meshVerts[0].pos);
        vertCoords.Add(meshVerts[1].pos);
        vertCoords.Add(meshVerts[2].pos);
        vertCoords.Add(meshVerts[3].pos);

        //Create tetrahedron mesh
        tetMesh = new Mesh();
        tetMesh.vertices = vertCoords.ToArray();
        tetMesh.SetIndices(new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 }, MeshTopology.Triangles, 0);
        tetMesh.RecalculateNormals();
        tetMesh.RecalculateBounds();

        //cache index of vertex opposed to the corresponding traingle in the collider face array. Used for normals calculation
        vertexOpposedFaces[0] = 3;
        vertexOpposedFaces[1] = 1;
        vertexOpposedFaces[2] = 2;
        vertexOpposedFaces[3] = 0;

        //create mesh filter for rendering
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = tetMesh;

        //add new collider to gameobject
        tetCollider = gameObject.AddComponent<MeshCollider>();
        tetCollider.sharedMesh = tetMesh;
        tetCollider.convex = true;
    }

    /// <summary>
    /// Computes the volume of the tetrahedron
    /// </summary>
    void ComputeVolume()
    {
        var a = VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[1].pos - meshVerts[0].pos);
        var b = VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[2].pos - meshVerts[0].pos);
        var c = VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(meshVerts[3].pos - meshVerts[0].pos);
        vol = (1 / 6) * (VectorUtils.CrossProduct(a, b)) * c;
    }

    /// <summary>
    /// Computes the centroid of the tetrahedron
    /// </summary>
    void ComputeCentroid()
    {
        Vector3 sum = Vector3.zero;
        foreach (FemVert v in meshVerts)
        {
            sum += v.pos;
        }
        centroid = sum / 4;
    }

    /// <summary>
    /// Computes the tetrahedron face normals
    /// </summary>
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

    /// <summary>
    /// Draw the tetrahedron face normals 
    /// </summary>
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

    /// <summary>
    /// Draw the tetrahedron mesh collider
    /// </summary>
    /// <param name="color">Color to be used for line drawing</param>
    public void DrawMeshCollider(Color color)
    {
        List<Vector3> verts = tetMesh.vertices.ToList();
        List<Vector3> wcf_verts = new List<Vector3>();
        foreach (var v in verts)
        {
            wcf_verts.Add(transform.TransformPoint(v));
        }

        Debug.DrawLine(wcf_verts[0], wcf_verts[1], color);
        Debug.DrawLine(wcf_verts[0], wcf_verts[2], color);
        Debug.DrawLine(wcf_verts[0], wcf_verts[3], color);
        Debug.DrawLine(wcf_verts[1], wcf_verts[2], color);
        Debug.DrawLine(wcf_verts[1], wcf_verts[3], color);
        Debug.DrawLine(wcf_verts[2], wcf_verts[3], color);        
    }

    //TODO use barycentric weights
    /// <summary>
    /// Distribute an impact force to the tetrahedron vertices using barycentrix weights
    /// </summary>
    /// <param name="f">Force the colliding objects applies to the tetrahedron</param>
    public void ApplyCollisionForceToNodes(Vector3 f)
    {
        foreach (FemVert v in meshVerts)
        {
            v.Fi += VectorUtils.ConvertUnityVec3ToMathNetNumericsVec3(f);
        }

        StartCoroutine(parentFemMesh.EnableFractureComputation());
    }

    /// <summary>
    /// Checks if a points is inside or on the boundary of the tetrahedron.
    /// Based on Ericson, C. (2005). Real-time collision detection (1st ed.). 
    /// Morgan Kaufmann Publishers. Page 48, Chapter 3.4: Barycentric coordinates.
    /// </summary>
    /// <param name="p">Point in world coordinates</param>
    /// <returns>True if the point is in the tetrahedron or on its boundary, false otherwise</returns>
    public bool ContainsPoint(Vector3 p)
    {
        var a = transform.TransformPoint(meshVerts[0].pos);
        var b = transform.TransformPoint(meshVerts[1].pos);
        var c = transform.TransformPoint(meshVerts[2].pos);
        var d = transform.TransformPoint(meshVerts[3].pos);

        var PBCD = MathNetNumerics.Matrix<float>.Build.Dense(4,4);
        PBCD.SetColumn(0, new float[] { p.x, p.y, p.z, 1 });
        PBCD.SetColumn(1, new float[] { b.x, b.y, b.z, 1 });
        PBCD.SetColumn(2, new float[] { c.x, c.y, c.z, 1 });
        PBCD.SetColumn(3, new float[] { d.x, d.y, d.z, 1 });

        var APCD = MathNetNumerics.Matrix<float>.Build.Dense(4, 4);
        APCD.SetColumn(0, new float[] { a.x, a.y, a.z, 1 });
        APCD.SetColumn(1, new float[] { p.x, p.y, p.z, 1 });
        APCD.SetColumn(2, new float[] { c.x, c.y, c.z, 1 });
        APCD.SetColumn(3, new float[] { d.x, d.y, d.z, 1 });

        var ABPD = MathNetNumerics.Matrix<float>.Build.Dense(4, 4);
        ABPD.SetColumn(0, new float[] { a.x, a.y, a.z, 1 });
        ABPD.SetColumn(1, new float[] { b.x, b.y, b.z, 1 });
        ABPD.SetColumn(2, new float[] { p.x, p.y, p.z, 1 });
        ABPD.SetColumn(3, new float[] { d.x, d.y, d.z, 1 });

        var ABCD = MathNetNumerics.Matrix<float>.Build.Dense(4, 4);
        ABCD.SetColumn(0, new float[] { a.x, a.y, a.z, 1 });
        ABCD.SetColumn(1, new float[] { b.x, b.y, b.z, 1 });
        ABCD.SetColumn(2, new float[] { c.x, c.y, c.z, 1 });
        ABCD.SetColumn(3, new float[] { d.x, d.y, d.z, 1 });

        var detPBCD = PBCD.Determinant();
        var detAPCD = APCD.Determinant();
        var detABPD = ABPD.Determinant();
        var detABCD = ABCD.Determinant();

        var u = detPBCD / detABCD;
        var v = detAPCD / detABCD;
        var w = detABPD / detABCD;
        var x = 1 - u - v - w;

        if (u > 0 && v > 0 && w > 0 && x > 0) return true;
        else return false;
    }
}