//#define DEBUG_MODE_ON

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class FemMesh : MonoBehaviour
{
    static int id = 0;
    public static int maxFractureEventsCount = 5;
    public static int curFractureEventsCount = 0;
    public static bool fractureCoroutineCalled = false;

    [Header("DebugDraw")]
    [SerializeField] bool drawTets = false;
    [SerializeField] Color color = Color.cyan;

    [Header("Input Mesh Data")]
    [SerializeField] string tetsFilePath;
    [SerializeField] string vertsFilePath;
    [SerializeField] MeshSplinters meshSplinters;

    [Header("Simulation Parameters")]
    public InitializationMode initializationMode = InitializationMode.RegularStart; //default mode
    public float femElementMass = 42f;
    public bool computeFracture = false;
    public float computeFractureTimeWindow = 0.01f;
    public int meshContinuityMaxSearchDepth = 2;

    [Header("FEM Elements")]
    public List<Tetrahedron> tets = new List<Tetrahedron>();
    public List<FemVert> verts = new List<FemVert>();

    [Header("Splinters")]
    public List<GameObject> brickPrefabs = new List<GameObject>();
    List<GameObject> instantiatedBricks = new List<GameObject>();

    //parsed data
    List<Vector3> rawVerts;
    List<int[]> rawTets;

    //flag for mesh continuity computation
    bool searchCancelled = false;

    void Start()
    {
        if (initializationMode == InitializationMode.RegularStart)
        {
            RegularInitialization();
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = tets.Count * femElementMass;
        }
        else
        {
            ValidateMeshBoundaries();
        }
    }

    void Update()
    {
        //draw tet colliders if flag is true
        if (drawTets) tets.Where(tet => drawTets).ToList().ForEach(tet => tet.DrawMeshCollider(color));
    }

    void LateUpdate()
    {
        curFractureEventsCount = 0;
        FemMesh.fractureCoroutineCalled = false;
    }

    /// <summary>
    /// Initializes the FemMesh by parsing FEM data and creating respective
    /// tetrahedra and vertex objects.
    /// </summary>
    void RegularInitialization()
    {
        TetrahedralmeshParser.ParseTetMeshFiles(Application.dataPath + vertsFilePath,
            Application.dataPath + tetsFilePath,
            out rawVerts, out rawTets);

        //create fem vert objects
        GameObject vertsParent = new GameObject();
        vertsParent.transform.parent = gameObject.transform;
        vertsParent.transform.name = "verts";
        for (int i = 0; i<rawVerts.Count(); i++)
        {
            var vert = rawVerts[i];
            var vertGo = new GameObject("v" + i);
            vertGo.transform.parent = vertsParent.transform;
            var fem_vert = vertGo.AddComponent<FemVert>();
            fem_vert.pos = vert;
            fem_vert.parentFemMesh = this;
#if DEBUG_MODE_ON
            fem_vert.id = vertGo.transform.name;
#endif

            verts.Add(fem_vert);
        }

        //create tet objects
        GameObject tetsParent = new GameObject();
        tetsParent.transform.parent = gameObject.transform;
        tetsParent.transform.name = "tets";

        int id = 0;
        foreach (int[] values in rawTets)
        {
            //get the tets verts indices
            int i0 = values[0];
            int i1 = values[1];
            int i2 = values[2];
            int i3 = values[3];

            FemVert fem_v0 = verts[i0];
            FemVert fem_v1 = verts[i1];
            FemVert fem_v2 = verts[i2];
            FemVert fem_v3 = verts[i3];

            GameObject go = new GameObject("tet_go (" + id++ + ")");
            Tetrahedron tet = go.AddComponent<Tetrahedron>();
            tet.meshVerts = new List<FemVert> { fem_v0, fem_v1, fem_v2, fem_v3 };
            tets.Add(tet);
            tet.parentFemMesh = this;

            fem_v0.tets.Add(tet);
            fem_v1.tets.Add(tet);
            fem_v2.tets.Add(tet);
            fem_v3.tets.Add(tet);

            go.transform.parent = tetsParent.transform;
        }

        //create splinters and assign each to a tet
        var splintersParent = new GameObject("splinters");
        splintersParent.transform.parent = gameObject.transform;
        var spawner = ScriptableObject.CreateInstance<BrickSpawner>();
        if (meshSplinters == MeshSplinters.SmallWall)
        {
            instantiatedBricks = spawner.SpawnSmallWallSplinters(splintersParent, brickPrefabs);
        }
        else if (meshSplinters == MeshSplinters.MediumWall)
        {
            instantiatedBricks = spawner.SpawnMediumWallSplinters(splintersParent, brickPrefabs);
        }

        foreach (Tetrahedron tet in tets)
        {
            List<GameObject> bricksInTet = new List<GameObject>();
            foreach (GameObject brick in instantiatedBricks)
            {
                if (tet.ContainsPoint(transform.TransformPoint(brick.transform.position)))
                {
                    bricksInTet.Add(brick);
                }
            }

            foreach (GameObject brick in bricksInTet)
            {
                brick.transform.parent = tet.gameObject.transform;
            }

            bricksInTet.ForEach(b => instantiatedBricks.Remove(b));
        }
    }

    /// <summary>
    /// Validate mesh continuity such that no 2 tetrahedra are connected only
    /// by a path passing through a joint (1 shared vertex) or hinge (2 shared vertices)
    /// If mesh is invalid then it is split in 2 valid sets.
    /// </summary>
    void ValidateMeshBoundaries()
    {
        foreach(Tetrahedron tet in tets)
        {
            foreach (Tetrahedron tet2 in tets)
            {
                if (tet == tet2) continue;

                var sharedVerts = tet.meshVerts.Intersect(tet2.meshVerts);

                if (sharedVerts.Count() == 1 || sharedVerts.Count() == 2)
                {
                    //compute the sets of tetrahedra satisfying the mesh continuity condition
                    var leftSideSet = ComputeMeshConnectedSet(tet, new List<Tetrahedron>() { tet }, 
                        new List<Tetrahedron>(), new List<FemVert>(), 0);

                    if (searchCancelled)
                    {
                        //path between both tets couldn't be found within heuristic so we separate the mesh
                        searchCancelled = false;

                        FractureMesh(tets.Except(new List<Tetrahedron>() { tet2 }).ToList(),
                            new List<Tetrahedron>() { tet2 });
                        return;
                    }
                    else
                    {
                        if (leftSideSet.Contains(tet2))
                        {
                            //this means despite the 2 tets sharing 1 or 2 vertices,
                            //there is an indirect path between them that is valid
                            continue;
                        }
                        else
                        {
                            //no valid path between tetrahedra, separate mesh
                            FractureMesh(leftSideSet, tets.Except(leftSideSet).ToList());
                            return;
                        }
                    }

                }
            }
        }
    }

    /// <summary>
    /// Computes the set of tetrahedra that satisfy the continuity condition that
    /// states that all tetrahedra in the mesh must share 3 vertices with their neighbors.
    /// </summary>
    /// <param name="curTet">Current tetrahedron that's being evaluated</param>
    /// <param name="set">Set of tetrahedra that satisy the continuity condition</param>
    /// <param name="visitedTets">List of already visited tetrahedra</param>
    /// <param name="visitedVerts">List of already visited vertices</param>
    /// <returns>The updated set of tetrahedra that satisy the continuity condition</returns>
    public List<Tetrahedron> ComputeMeshConnectedSet(Tetrahedron curTet, List<Tetrahedron> set, 
        List<Tetrahedron> visitedTets, List<FemVert> visitedVerts, int currentDepth)
    {
        if (currentDepth >= meshContinuityMaxSearchDepth)
        {
            searchCancelled = true;
            return set;
        }

        if (visitedTets.Contains(curTet)) return set;
        visitedTets.Add(curTet);

        foreach (FemVert v in curTet.meshVerts)
        {
            if (visitedVerts.Contains(v)) continue;
            visitedVerts.Add(v);

            foreach (Tetrahedron tet in v.tets)
            {
                if (!set.Contains(tet))
                {
                    var sharedVerts = tet.meshVerts.Intersect(curTet.meshVerts);
                    if (sharedVerts.Count() == 3)
                    {
                        set.Add(tet);
                        if (!visitedTets.Contains(tet))
                        {
                            if (searchCancelled) return set;

                            ComputeMeshConnectedSet(tet, set, visitedTets, visitedVerts, currentDepth++);
                        }
                    }
                }
            }
        }

        return set;
    }



    /// <summary>
    /// Separates a finite element method mesh into two parts defined by leftSide and rightSide.
    /// <para></para>* The leftSide list contains all tetrahedrons on the left side of the fracture plane.
    /// <para></para>* The rightSide list contains all tetrahedrons on the right side of the fracture plane.
    /// </summary>
    /// <param name="leftSide">Contains all tetrahedrons on the left side of the fracture plane</param>
    /// <param name="rightSide">Contains all tetrahedrons on the right side of the fracture plane</param>
    public void FractureMesh(List<Tetrahedron> leftSide, List<Tetrahedron> rightSide)
    {        
        //init
        var leftParent = new GameObject("Tets Mesh (" + id++ + ")");
        leftParent.transform.parent = gameObject.transform.parent.transform;
        var leftTetsParent = new GameObject("tets");
        leftTetsParent.transform.parent = leftParent.transform;
        var left_FEM = leftParent.AddComponent<FemMesh>();
        left_FEM.initializationMode = InitializationMode.FractureStart;
        //assign tetrahedra to new FemMesh and update relations
        left_FEM.tets = leftSide;
        foreach (Tetrahedron tet in leftSide)
        {
            tet.gameObject.transform.parent = leftTetsParent.transform;
            tet.parentFemMesh = left_FEM;
        }

        //same as we just did, but for the other FemMesh
        var rightParent = new GameObject("Tets Mesh (" + id++ + ")");
        rightParent.transform.parent = gameObject.transform.parent.transform;
        var rightTetsParent = new GameObject("tets");
        rightTetsParent.transform.parent = rightParent.transform;
        var right_FEM = rightParent.AddComponent<FemMesh>();
        right_FEM.initializationMode = InitializationMode.FractureStart;
        right_FEM.tets = rightSide;
        foreach (Tetrahedron tet in rightSide)
        {
            tet.gameObject.transform.parent = rightTetsParent.transform;
            tet.parentFemMesh = right_FEM;
        }


        //compute the vertices used in each of the new FemMeshes
        List<FemVert> leftVerts = new List<FemVert>();
        left_FEM.tets.ForEach(t => leftVerts.AddRange(t.meshVerts));
        List<FemVert> rightVerts = new List<FemVert>();
        right_FEM.tets.ForEach(t => rightVerts.AddRange(t.meshVerts));

        //compute shared vertices
        var intersection = leftVerts.Intersect(rightVerts).ToList();

        //duplicate shared vertices and recompute vertex sets
        foreach(FemVert v in intersection)
        {
            var vertCopyGo = GameObject.Instantiate(v.gameObject);
            foreach (Tetrahedron tet in right_FEM.tets)
            {
                if (tet.meshVerts.Contains(v))
                {
                    var index = tet.meshVerts.IndexOf(v);
                    tet.meshVerts[index] = vertCopyGo.GetComponent<FemVert>();
                }
            }
        }
        right_FEM.UpdateVerts();

        left_FEM.verts = leftVerts.Distinct().ToList();
        left_FEM.UpdateVerts();

        //inherit current mesh velocity
        var inheritedVelocity = gameObject.GetComponent<Rigidbody>().velocity;
        var left_FEM_rb = left_FEM.AddComponent<Rigidbody>();
        left_FEM_rb.velocity = inheritedVelocity;
        left_FEM_rb.mass = left_FEM.tets.Count * left_FEM.femElementMass;

        var right_FEM_rb = right_FEM.AddComponent<Rigidbody>();
        right_FEM_rb.velocity = inheritedVelocity;
        right_FEM_rb.mass = right_FEM.tets.Count * right_FEM.femElementMass;

        //destroy the old FemMesh
        Destroy(gameObject);
    }

    /// <summary>
    /// Updates FemMesh vertex data. Called automatically after FractureMesh.
    /// </summary>
    void UpdateVerts()
    {
        verts.Clear();
        tets.ForEach(t => verts.AddRange(t.meshVerts));

        GameObject vertsParent = new GameObject("verts");
        vertsParent.transform.parent = gameObject.transform;

        verts.ForEach(v => v.gameObject.transform.parent = vertsParent.transform);
        verts = verts.Distinct().ToList();

        //update the tetrahedra referenced by the vertices
        foreach(FemVert v in verts)
        {
            v.tets.Clear();
            v.parentFemMesh = this;
        }
        foreach (Tetrahedron tet in tets)
        {
            foreach (FemVert v in tet.meshVerts)
            {
                v.tets.Add(tet);
            }
        }
    }

    //TODO this might not be a good idea
    /// <summary>
    /// Coroutine that opens a time window to compute fractures in the mesh
    /// </summary>
    /// <returns></returns>
    public IEnumerator EnableFractureComputation()
    {
        computeFracture = true;
        yield return new WaitForSecondsRealtime(computeFractureTimeWindow);
        computeFracture = false;
    }

    public enum InitializationMode
    {
        RegularStart,
        FractureStart
    }

    public enum MeshSplinters
    {
        SmallWall,
        MediumWall
    }
}


