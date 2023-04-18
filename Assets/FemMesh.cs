using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

public class FemMesh : MonoBehaviour
{
    [SerializeField] bool drawTets = true;
    public bool parseInit = false;
    static int id = 0;
    public Color color = Color.red;
    public bool computeFracture = false;

    public bool bricksAssigned = true;
    public List<GameObject> brickPrefabs = new List<GameObject>();
    public List<GameObject> instantiatedBricks = new List<GameObject>();

    public int maxFractureEventsCount = 10;
    public int curFractureEventsCount = 0;

    // Define a list to store the parsed data
    public List<Vector3> verts_data = new List<Vector3>();
    public List<int[]> tets_data = new List<int[]>();
    public List<Tetrahedron> tets = new List<Tetrahedron>();

    //FEM data structures
    public List<FemVert> verts = new List<FemVert>();

    //Tet and separating force pair
    public bool applyImpulseOnStart = false;
    public Tetrahedron targetTet;
    public Vector3 separatingForce;

    void Start()
    {
        if (parseInit) ParseInit();

        var rb = gameObject.AddComponent<Rigidbody>();
        var totalMass = 0f;
        tets.ForEach(t => totalMass += t.mass);
        rb.mass = totalMass;

        if (applyImpulseOnStart)
        {
            //applyImpulseOnStart= false;
            //rb.AddForce(separatingForce, ForceMode.Impulse);
            //separatingForce = Vector3.zero;
            StartCoroutine(EnableFractureComputation());
        }
    }

    void Update()
    {
        if (drawTets) tets.Where(tet => drawTets).ToList().ForEach(tet => tet.DrawMesh(color));
    
        if (tets.Count == 0 && verts.Count == 0) { Destroy(gameObject); }

        curFractureEventsCount = 0;
    }

    //called by a vert that has a fracture plane dividing the mesh in 2 non-empty sets
    public void FractureMesh(List<Tetrahedron> leftSide, List<Tetrahedron> rightSide)
    {
        //partition the tets
        var leftParent = new GameObject("Tets Mesh (" + id++ + ")");
        var leftTetsParent = new GameObject("tets");
        leftTetsParent.transform.parent = leftParent.transform;
        var left_FEM = leftParent.AddComponent<FemMesh>();
        left_FEM.tets = leftSide;
        foreach (Tetrahedron tet in leftSide)
        {
            tet.gameObject.transform.parent = leftTetsParent.transform;
            tet.parentFemMesh = left_FEM;
        }

        var rightParent = new GameObject("Tets Mesh (" + id++ + ")");
        var rightTetsParent = new GameObject("tets");
        rightTetsParent.transform.parent = rightParent.transform;
        var right_FEM = rightParent.AddComponent<FemMesh>();
        right_FEM.tets = rightSide;
        right_FEM.color = Color.cyan;
        foreach (Tetrahedron tet in rightSide)
        {
            tet.gameObject.transform.parent = rightTetsParent.transform;
            tet.parentFemMesh = right_FEM;
        }

        tets.RemoveAll(t => leftSide.Contains(t));
        tets.RemoveAll(t => rightSide.Contains(t));

        //partition the verts
        List<FemVert> leftVerts = new List<FemVert>();
        left_FEM.tets.ForEach(t => leftVerts.AddRange(t.meshVerts));

        List<FemVert> rightVerts = new List<FemVert>();
        right_FEM.tets.ForEach(t => rightVerts.AddRange(t.meshVerts));

        var intersection = leftVerts.Intersect(rightVerts).ToList();
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

        //GameObject leftVertsParent = new GameObject("verts");
        //leftVertsParent.transform.parent = left_FEM.gameObject.transform;
        //foreach (FEM_Vert v in leftVerts)
        //{
        //    v.gameObject.transform.parent = leftVertsParent.transform;
        //}
        left_FEM.verts = leftVerts.Distinct().ToList();
        left_FEM.UpdateVerts();

        if (left_FEM.tets.Contains(targetTet))
        {
            left_FEM.separatingForce = separatingForce;
            left_FEM.applyImpulseOnStart = true;
        }
        else
        {
            right_FEM.separatingForce = separatingForce;
            right_FEM.applyImpulseOnStart = true;
        }
        verts.Clear();
    }

    public void UpdateVerts()
    {
        verts.Clear();
        tets.ForEach(t => verts.AddRange(t.meshVerts));

        GameObject vertsParent = new GameObject("verts");
        vertsParent.transform.parent = gameObject.transform;

        verts.ForEach(v => v.gameObject.transform.parent = vertsParent.transform);
        verts = verts.Distinct().ToList();

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

    void ParseInit()
    {
        TetrahedralmeshParser.ParseTetMeshFiles(this, Application.dataPath + "/Tetrahedral Mesh Data/verts.csv",
            Application.dataPath + "/Tetrahedral Mesh Data/tets.csv");

        //create fem vert objects
        GameObject vertsParent = new GameObject();
        vertsParent.transform.parent = gameObject.transform;
        vertsParent.transform.name = "verts";

        int i = 0;
        foreach (Vector3 vert in verts_data)
        {
            var vertGo = new GameObject("v" + i++);
            vertGo.transform.parent = vertsParent.transform;
            var fem_vert = vertGo.AddComponent<FemVert>();
            fem_vert.coords = vert;
            verts.Add(fem_vert);
            fem_vert.parentFemMesh = this;
            fem_vert.id = vertGo.transform.name;
        }

        //create tet objects
        GameObject tetsParent = new GameObject();
        tetsParent.transform.parent = gameObject.transform;
        tetsParent.transform.name = "tets";

        int id = 0;
        foreach (int[] values in tets_data)
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

        var splintersParent = new GameObject("splinters");
        splintersParent.transform.parent = gameObject.transform;
        var spawner = ScriptableObject.CreateInstance<BrickSpawner>();
        spawner.SpawnBricks(splintersParent, brickPrefabs);

        instantiatedBricks = spawner.instantiatedBricks;
        bricksAssigned = false;
        foreach (Tetrahedron tet in tets)
        {
            List<GameObject> bricksInTet = new List<GameObject>();
            foreach (GameObject brick in instantiatedBricks)
            {
                if(tet.IsPointInside(transform.TransformPoint(brick.transform.position)))
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

    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint cp in collision.contacts)
        {
            if (cp.otherCollider.GetComponent<FemMesh>() != null)
            {
                Debug.Log("FEM MESH TO FEM MESH COLLISION");
            }
        }
    }
    public IEnumerator EnableFractureComputation()
    {
        computeFracture = true;
        yield return new WaitForSeconds(0.25f);
        computeFracture = false;
    }
}
