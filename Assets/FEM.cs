using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class FEM : MonoBehaviour
{
    [SerializeField] bool drawTets = true;
    [SerializeField] bool drawBBs = false;

    // Define a list to store the parsed data
    public List<Vector3> verts_data = new List<Vector3>();
    public List<int[]> tets_data = new List<int[]>();
    public List<Tetrahedron> tets = new List<Tetrahedron>();

    //root bounding box bounds
    public BoxCollider boxCollider;
    public BoundingBox rootBox;
    public float minX, minY, minZ, maxX, maxY, maxZ;

    void Start()
    {
        TetrahedralmeshParser.ParseTetMeshFiles(this, Application.dataPath + "/Tetrahedral Mesh Data/verts.csv",
            Application.dataPath + "/Tetrahedral Mesh Data/tets.csv");

        rootBox = new BoundingBox(minX, minX, minX, maxX, maxY, maxZ);
        rootBox.isRoot= true;

        BoundingBoxTree.VerticalSplit(rootBox, 2);

        //create a collideable item of the rootbox
        var ccd = GameObject.Find("CCD").GetComponent<CollisionDetection>();
        ccd.AddCollisionClient(rootBox);
    }

    void Update()
    {
        if (drawBBs) BoundingBoxTree.Draw(rootBox);

        tets.Where(tet => drawTets).ToList().ForEach(tet => tet.Draw());
    }
}
