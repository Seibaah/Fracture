//#define DEBUG_MODE_ON

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class FemMesh : MonoBehaviour
{
    static int id = 0;

    [Header("DebugDraw")]
    [SerializeField] bool drawTets = true;
    [SerializeField] Color color = Color.red;

    [Header("Simulation Parameters")]
    public InitializationMode initializationMode = InitializationMode.RegularStart; //default mode
    public int maxFractureEventsCount = 10; //per frame
    public int curFractureEventsCount = 0;
    public float femElementMass = 42f;
    public bool computeFracture = false;
    public float computeFractureTimeWindow = 0.25f;

    [Header("FEM Elements")]
    public List<Tetrahedron> tets = new List<Tetrahedron>();
    public List<FemVert> verts = new List<FemVert>();

    [Header("Splinters")]
    public List<GameObject> brickPrefabs = new List<GameObject>();
    List<GameObject> instantiatedBricks = new List<GameObject>();

    //parsed data
    List<Vector3> rawVerts;
    List<int[]> rawTets;

    void Start()
    {
        if (initializationMode == InitializationMode.RegularStart)
        {
            RegularInitialization();
        }
        else
        {
            StartCoroutine(EnableFractureComputation());
            ValidateMeshBoundaries();
        }

        //add a rigidbody and compute its mass
        var rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = tets.Count * femElementMass;
    }

    void Update()
    {
        //draw tet colliders if flag is true
        if (drawTets) tets.Where(tet => drawTets).ToList().ForEach(tet => tet.DrawMeshCollider(color));

        curFractureEventsCount = 0;
    }

    /// <summary>
    /// Initializes the FemMesh by parsing FEM data and creating respective
    /// tetrahedra and vertex objects.
    /// </summary>
    void RegularInitialization()
    {
        TetrahedralmeshParser.ParseTetMeshFiles(Application.dataPath + "/Tetrahedral Mesh Data/verts.csv",
            Application.dataPath + "/Tetrahedral Mesh Data/tets.csv",
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
        instantiatedBricks = spawner.SpawnBricksDemo_1(splintersParent, brickPrefabs);

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
    /// Validates that no 2 tetrahedra in the mesh are connected by only an edge (hinge)
    /// or point (joint). If this scenario is detected the mesh is fractured in 2 sets 
    /// to solve the boundary issue
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
                    var sharedVertsList = sharedVerts.ToArray();
                    Vector3 originPoint;
                    if (sharedVerts.Count() == 2) {
                        originPoint = Vector3.Lerp(sharedVertsList[0].pos, sharedVertsList[1].pos, 0.5f);
                    }
                    else
                    {
                        originPoint = sharedVertsList[0].pos;
                    }

                    var fracturePlane = new Plane(Vector3.right, originPoint);

                    //compute tetrahedra left and right of the plane
                    List<Tetrahedron> leftSide = new List<Tetrahedron>();
                    List<Tetrahedron> rightSide = new List<Tetrahedron>();
                    foreach (Tetrahedron t in tets)
                    {
                        if (fracturePlane.GetSide(t.centroid) == true) rightSide.Add(t);
                        else leftSide.Add(t);
                    }

                    //plane must divide mesh in 2 non-empty sets to cause fracture
                    if (leftSide.Count > 0 && rightSide.Count > 0)
                    {
                        FractureMesh(leftSide, rightSide);
                    }

                    return;
                }
            }
        }
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
}


