using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MNetNumerics = MathNet.Numerics.LinearAlgebra;

public class FEM_Vert : MonoBehaviour
{
    //debug
    [SerializeField]
    private Vector3 Fi_Debug = new Vector3();
    static int fractureLeft = 5;

    //parent fem mesh
    public FEM_Mesh parentMesh;
    //tets incident on this vertex
    public List<Tetrahedron> tets = new List<Tetrahedron>(); 

    //elastic force exerted by all incident tets on this vertex
    public MNetNumerics.Vector<float> Fi = MNetNumerics.Vector<float>.Build.Dense(3); 

    //tensile component of the force and its set
    public MNetNumerics.Vector<float> FiPlus = MNetNumerics.Vector<float>.Build.Dense(3);
    public List<MNetNumerics.Vector<float>> SetFiPlus = new List<MNetNumerics.Vector<float>>();

    //compressive component of the force and its set
    public MNetNumerics.Vector<float> FiMin = MNetNumerics.Vector<float>.Build.Dense(3);
    public List<MNetNumerics.Vector<float>> SetFiMin = new List<MNetNumerics.Vector<float>>();

    //separation tensor
    public MNetNumerics.Matrix<float> separationTensor = MNetNumerics.Matrix<float>.Build.Dense(3, 3);

    //physical properties of the vertex
    public Vector3 coords;
    public float k = 1.9f; //young modulus, in GPa
    public float v = 0.41f; //poisson ratio;
    public float tau = 0.1f; //fracture threshold

    void Start()
    {
        gameObject.transform.position = coords;
        gameObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        gameObject.transform.parent = this.gameObject.transform;
    }

    void Update()
    {
        coords = transform.position;
        Fi_Debug = VectorUtils.ConvertNumericsVec3ToUnity(Fi);
        ComputeSeparationTensor();

        //Debug.Log(separationTensor.ToString());
        var st_evd = separationTensor.Evd();
        var st_eigenval = st_evd.EigenValues;
        var st_eigenvec = st_evd.EigenVectors;

        var maxEigenval = st_eigenval[st_eigenval.Count- 1];
        //Debug.Log("Max eigenval: " +maxEigenval);
        if (maxEigenval.Real > tau && fractureLeft > 0)
        {
            fractureLeft--;
            var maxEigenvec = st_eigenvec.Column(st_eigenval.Count - 1);

            //FlagAffectedVerts();

            //separate mesh along the plane
            var fracturePlane = new Plane(VectorUtils.ConvertNumericsVec3ToUnity(maxEigenvec), coords);
            //Debug.Log("fracture origin vert " + this.gameObject.transform.name);
            Debug.DrawRay(coords, VectorUtils.ConvertNumericsVec3ToUnity(maxEigenvec), Color.blue);

            var allTets = parentMesh.tets;
            List<Tetrahedron> leftSide = new List<Tetrahedron>();
            List<Tetrahedron> rightSide = new List<Tetrahedron>();
            foreach (Tetrahedron tet in allTets)
            {
                if (v.Equals(this)) continue;

                if (fracturePlane.GetSide(tet.centroid) == true) rightSide.Add(tet);
                else leftSide.Add(tet);
            }

            if (leftSide.Count > 0 && rightSide.Count > 0)
            {
                parentMesh.FractureMesh(leftSide, rightSide);
            }
            else
            {
                fractureLeft++;
            }
        }
    }

    void ComputeSeparationTensor()
    {
        MNetNumerics.Matrix<float> mFiPlus = ComputeOperatorM(FiPlus);
        MNetNumerics.Matrix<float> mSetFiPlusSum = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < SetFiPlus.Count; i++)
        {
            mSetFiPlusSum += ComputeOperatorM(SetFiPlus[i]);
        }

        MNetNumerics.Matrix<float> mFiMin = ComputeOperatorM(FiMin);
        MNetNumerics.Matrix<float> mSetFiMinSum = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < SetFiPlus.Count; i++)
        {
            mSetFiMinSum += ComputeOperatorM(SetFiPlus[i]);
        }

        separationTensor = 0.5f * (-mFiPlus + mSetFiPlusSum + mFiMin - mSetFiMinSum);
    }

    //computes the m(a) operator defined in Parker and O'Brien's paper
    MNetNumerics.Matrix<float> ComputeOperatorM(MNetNumerics.Vector<float> a)
    {
        if (a.At(0) == 0 && a.At(1) == 0 && a.At(2) == 0)
        {
            return MNetNumerics.Matrix<float>.Build.Sparse(3, 3);
        }
        else
        {
            return (a.ToColumnMatrix() * a.ToRowMatrix()) / (float)a.L2Norm();
        }
    }

    void FlagAffectedVerts()
    {
        //Debug.Log("Fracture, eigenval: " + maxEigenval.Real);
        Debug.Log("Fracture vert" + gameObject.transform.name);
        //foreach (Tetrahedron tet in tets)
        //{
        //    if (tet.tetRendered == false) tet.RenderTet();
        //}
    }
}
