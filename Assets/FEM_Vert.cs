using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MNetNumerics = MathNet.Numerics.LinearAlgebra;

public class FEM_Vert : MonoBehaviour
{
    //debug
    [SerializeField]
    private Vector3 Fi_Debug = new Vector3();

    public List<Tetrahedron> tets = new List<Tetrahedron>();
    public MNetNumerics.Vector<float> Fi = MNetNumerics.Vector<float>.Build.Dense(3);

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
    }
}
