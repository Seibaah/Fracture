using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class FEM_Mesh : MonoBehaviour
{
    [SerializeField] bool drawTets = true;
    public bool parseInit = false;
    static int id = 0;
    public Color color = Color.red;

    // Define a list to store the parsed data
    public List<Vector3> verts_data = new List<Vector3>();
    public List<int[]> tets_data = new List<int[]>();
    public List<Tetrahedron> tets = new List<Tetrahedron>();

    //FEM data structures
    public List<FEM_Vert> verts = new List<FEM_Vert>();

    void Start()
    {
        if (parseInit) ParseInit();

        var rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 1000f;
    }

    void Update()
    {
        if (drawTets) tets.Where(tet => drawTets).ToList().ForEach(tet => tet.DrawMesh(color));
    
        if (tets.Count == 0 && verts.Count == 0) { Destroy(gameObject); }
    }

    //called by a vert that has a fracture plane dividing the mesh in 2 non-empty sets
    public void FractureMesh(List<Tetrahedron> leftSide, List<Tetrahedron> rightSide)
    {
        //partition the tets
        var leftParent = new GameObject("Tets Mesh (" + id++ + ")");
        var leftTetsParent = new GameObject("tets");
        leftTetsParent.transform.parent = leftParent.transform;
        var left_FEM = leftParent.AddComponent<FEM_Mesh>();
        left_FEM.tets = leftSide;
        foreach (Tetrahedron tet in leftSide)
        {
            tet.gameObject.transform.parent = leftTetsParent.transform;
        }

        var rightParent = new GameObject("Tets Mesh (" + id++ + ")");
        var rightTetsParent = new GameObject("tets");
        rightTetsParent.transform.parent = rightParent.transform;
        var right_FEM = rightParent.AddComponent<FEM_Mesh>();
        right_FEM.tets = rightSide;
        right_FEM.color = Color.cyan;
        foreach (Tetrahedron tet in rightSide)
        {
            tet.gameObject.transform.parent = rightTetsParent.transform;
        }

        tets.RemoveAll(t => leftSide.Contains(t));
        tets.RemoveAll(t => rightSide.Contains(t));

        //partition the verts
        List<FEM_Vert> leftVerts = new List<FEM_Vert>();
        left_FEM.tets.ForEach(t => leftVerts.AddRange(t.meshVerts));

        List<FEM_Vert> rightVerts = new List<FEM_Vert>();
        right_FEM.tets.ForEach(t => rightVerts.AddRange(t.meshVerts));

        var intersection = leftVerts.Intersect(rightVerts).ToList();
        foreach(FEM_Vert v in intersection)
        {
            var vertCopyGo = GameObject.Instantiate(v.gameObject);
            foreach (Tetrahedron tet in right_FEM.tets)
            {
                if (tet.meshVerts.Contains(v))
                {
                    var index = tet.meshVerts.IndexOf(v);
                    tet.meshVerts[index] = vertCopyGo.GetComponent<FEM_Vert>();
                }
            }
        }
        right_FEM.UpdateVerts();

        GameObject leftVertsParent = new GameObject("verts");
        leftVertsParent.transform.parent = left_FEM.gameObject.transform;
        foreach (FEM_Vert v in leftVerts)
        {
            v.gameObject.transform.parent = leftVertsParent.transform;
        }
        left_FEM.verts = leftVerts.Distinct().ToList();

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
    }

    void ParseInit()
    {
        //TODO move out of FEM_Mesh
        TetrahedralmeshParser.ParseTetMeshFiles(this, Application.dataPath + "/Tetrahedral Mesh Data/verts.csv",
            Application.dataPath + "/Tetrahedral Mesh Data/tets.csv");

        //create fem vert objects
        GameObject vertsParent = new GameObject();
        vertsParent.transform.parent = gameObject.transform;
        vertsParent.transform.name = "verts";

        int i = 0;
        foreach (Vector3 vert in verts_data)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.name = "v" + i++;
            sphere.transform.parent = vertsParent.transform;
            var fem_vert = sphere.AddComponent<FEM_Vert>();
            fem_vert.coords = vert;
            verts.Add(fem_vert);
            fem_vert.parentMesh = this;
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

            FEM_Vert fem_v0 = verts[i0];
            FEM_Vert fem_v1 = verts[i1];
            FEM_Vert fem_v2 = verts[i2];
            FEM_Vert fem_v3 = verts[i3];

            GameObject go = new GameObject("tet_go (" + id++ + ")");
            Tetrahedron tet = go.AddComponent<Tetrahedron>();
            tet.meshVerts = new List<FEM_Vert> { fem_v0, fem_v1, fem_v2, fem_v3 };
            tets.Add(tet);

            fem_v0.tets.Add(tet);
            fem_v1.tets.Add(tet);
            fem_v2.tets.Add(tet);
            fem_v3.tets.Add(tet);

            go.transform.parent = tetsParent.transform;
        }
    }
}
