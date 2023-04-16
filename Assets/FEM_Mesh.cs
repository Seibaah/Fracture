using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class FEM_Mesh : MonoBehaviour
{
    [SerializeField] bool drawTets = false;

    // Define a list to store the parsed data
    public List<Vector3> verts_data = new List<Vector3>();
    public List<int[]> tets_data = new List<int[]>();
    public List<Tetrahedron> tets = new List<Tetrahedron>();

    //FEM data structures
    public List<FEM_Vert> verts= new List<FEM_Vert>();

    void Start()
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

    void Update()
    {
        if (drawTets) tets.Where(tet => drawTets).ToList().ForEach(tet => tet.DrawMesh());
    }
}
