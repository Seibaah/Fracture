using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FemMesh;

/// <summary>
/// Settings per instanced FemMesh
/// </summary>
[CreateAssetMenu(fileName = "FEM_Instance_Settings", menuName = "FemMesh Instance Settings")]
public class FemMeshInstanceSettings : ScriptableObject
{
    [Header("Material Parameters")]
    [Tooltip("Young's Modulus: measure of stiffness of the material (GPa)")] 
    public float youngModulus = 1.9f;
    [Tooltip("Poisson Ratio: measure of how much the material stretches or compresses")] 
    public float poissonRatio = 0.41f;
    [Tooltip("Material Toughness Threshold")] 
    public float toughness = 0.25f; //the higher the less brittle the material is

    [Header("Simulation Parameters")]
    [Tooltip("Tetrahedron Mass")] 
    public float femElementMass = 42f;
    [Tooltip("How long the fracture algorithm is allowed to run when triggered")] 
    public float computeFractureTimeWindow = 0.01f;
    [Tooltip("Search depth to determine 2 tetrahedra satisfy the mesh continuity Condition")] 
    public int meshContinuityMaxSearchDepth = 2;

    [Header("Splinters")]
    public List<GameObject> splinterPrefab = new List<GameObject>();
    public List<GameObject> instantiatedBricks = new List<GameObject>();

    [Header("DebugDraw")]
    public bool drawTets = false;
    public Color color = Color.cyan;
}
