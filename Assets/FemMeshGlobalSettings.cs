using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Settings ot be shared by all the FemMesh objects in a scene
/// </summary>
[CreateAssetMenu(fileName = "FEM_Global_Settings", menuName = "FemMesh Global Settings")]
public class FemMeshGlobalSettings : ScriptableObject
{
    [Header("Global Fracture Parameters")]
    [Tooltip("Max Number of Fracture Events per Frame")] 
    public int maxFractureEventsCount = 5;
    [Tooltip("Number of Fracture Events Processed in the Current Frame")] 
    public int curFractureEventsCount = 0;
    [Tooltip("Is the fracture algorithm already switch on")] 
    public bool fractureCoroutineCalled = false;
}
