//#define DEBUG_MODE_ON

using System.Collections.Generic;
using UnityEngine;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

public class FemVert : MonoBehaviour
{
#if DEBUG_MODE_ON
    [Header("Debug")]
    [SerializeField] Vector3 Fi_Debug = new Vector3();
    public string id;
#endif

    [Header("FEM Elements")]
    public Vector3 pos;
    public FemMesh parentFemMesh;
    public List<Tetrahedron> tets = new List<Tetrahedron>(); //tetrahedra incident on this vertex

    [Header("Total Elastic Force")]
    public MathNetNumerics.Vector<float> Fi = MathNetNumerics.Vector<float>.Build.Dense(3);

    [Header("Tensile Forces")]
    public MathNetNumerics.Vector<float> FiPlus = MathNetNumerics.Vector<float>.Build.Dense(3);
    public List<MathNetNumerics.Vector<float>> SetFiPlus = new List<MathNetNumerics.Vector<float>>();

    [Header("Compressive Forces")]
    public MathNetNumerics.Vector<float> FiMin = MathNetNumerics.Vector<float>.Build.Dense(3);
    public List<MathNetNumerics.Vector<float>> SetFiMin = new List<MathNetNumerics.Vector<float>>();

    [Header("Separation Tensor")]
    public MathNetNumerics.Matrix<float> separationTensor = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);

    [Header("Simulation Parameters")]
    public float k = 1.9f; //young modulus, in GPa
    public float v = 0.41f; //poisson ratio;
    public float tau = 0.25f; //material toughness threshold

    void Start()
    {
        gameObject.transform.position = pos;
    }

    void Update()
    {
        pos = transform.position;
        if (parentFemMesh.computeFracture)
        {
#if DEBUG_MODE_ON
            Fi_Debug = VectorUtils.ConvertNumericsVec3ToUnityVec3(Fi);
#endif
            ComputeSeparationTensor();

#if DEBUG_MODE_ON
            Debug.Log(separationTensor.ToString());
#endif
            //Compute eigenvalues of the separation tensor
            var st_evd = separationTensor.Evd();
            var st_eigenval = st_evd.EigenValues;
            var st_eigenvec = st_evd.EigenVectors;
            var maxEigenval = st_eigenval[st_eigenval.Count - 1];

            //fracture may occur if the max eigenvalue of the tensor exceeds the toughness threshold paramater
            //and if we haven't exceeded the allowed fracture events count for the current frame
            if (maxEigenval.Real > tau
                && parentFemMesh.curFractureEventsCount < parentFemMesh.maxFractureEventsCount)
            {
#if DEBUG_MODE_ON
                Debug.Log("Max eigenval: " + maxEigenval);
#endif
                //create a fracture plane whose normal is the eigenvector of the max eigenvalue of the separation tensor
                var maxEigenvec = st_eigenvec.Column(st_eigenval.Count - 1);
                var fracturePlane = new Plane(VectorUtils.ConvertMathNetNumericsVec3ToUnityVec3(maxEigenvec), pos);
#if DEBUG_MODE_ON
                Debug.Log("fracture origin vert " + this.gameObject.transform.name);
                Debug.DrawRay(pos, VectorUtils.ConvertNumericsVec3ToUnityVec3(maxEigenvec), Color.blue);
#endif
                //compute tetrahedra left and right of the plane
                var allTets = parentFemMesh.tets;
                List<Tetrahedron> leftSide = new List<Tetrahedron>();
                List<Tetrahedron> rightSide = new List<Tetrahedron>();
                foreach (Tetrahedron tet in allTets)
                {
                    if (v.Equals(this)) continue;

                    if (fracturePlane.GetSide(tet.centroid) == true) rightSide.Add(tet);
                    else leftSide.Add(tet);
                }

                //plane must divide mesh in 2 non-empty sets to cause fracture
                if (leftSide.Count > 0 && rightSide.Count > 0)
                {
                    parentFemMesh.FractureMesh(leftSide, rightSide);
                }
            }
        }
    }

    /// <summary>
    /// Computes the separation tensor defined in O'Brien, J. F., & Hodgins, J. K. (1999). 
    /// Graphical modeling and animation of brittle fracture. 
    /// Proceedings of the 26th annual conference on Computer graphics and interactive techniques, 27-34. ACM.
    /// </summary>
    void ComputeSeparationTensor()
    {
        MathNetNumerics.Matrix<float> mFiPlus = ComputeOperatorM(FiPlus);
        MathNetNumerics.Matrix<float> mSetFiPlusSum = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < SetFiPlus.Count; i++)
        {
            mSetFiPlusSum += ComputeOperatorM(SetFiPlus[i]);
        }

        MathNetNumerics.Matrix<float> mFiMin = ComputeOperatorM(FiMin);
        MathNetNumerics.Matrix<float> mSetFiMinSum = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);
        for (int i = 0; i < SetFiPlus.Count; i++)
        {
            mSetFiMinSum += ComputeOperatorM(SetFiPlus[i]);
        }

        separationTensor = 0.5f * (-mFiPlus + mSetFiPlusSum + mFiMin - mSetFiMinSum);
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
        if (a.At(0) == 0 && a.At(1) == 0 && a.At(2) == 0)
        {
            return MathNetNumerics.Matrix<float>.Build.Sparse(3, 3);
        }
        else
        {
            return (a.ToColumnMatrix() * a.ToRowMatrix()) / (float)a.L2Norm();
        }
    }
}
