using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEM_Vert : MonoBehaviour
{
    public Vector3 coords;
    public float youngModulus = 1.9f; //in GPa
    public float poissonRatio = 0.41f;

    void Start()
    {
        gameObject.transform.position = coords;
        gameObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        gameObject.transform.parent = this.gameObject.transform;
    }

    void Update()
    {
        coords = transform.position;
    }
}
