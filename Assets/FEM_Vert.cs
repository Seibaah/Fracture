using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MNetNumerics = MathNet.Numerics.LinearAlgebra;

public class FEM_Vert : MonoBehaviour
{
    //debug
    [SerializeField]
    private Vector3 Fi_Debug = new Vector3();

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

    //computes the m operator defined in the Parker and O'Brien paper
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
}
