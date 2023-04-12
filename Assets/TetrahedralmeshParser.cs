using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TetrahedralmeshParser
{
    public static void ParseTetMeshFiles(FEM fem, string verts_path, string tets_path)
    {
        // Parse verts file
        using (StreamReader reader = new StreamReader(verts_path))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] fields = line.Split(',');
                float[] values = new float[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    values[i] = float.Parse(fields[i]);
                }
                fem.verts_data.Add(new Vector3(values[0], values[1], values[2]));
            }
        }

        // parse indexed tets file
        using (StreamReader reader = new StreamReader(tets_path))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] fields = line.Split(',');
                int[] values = new int[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    values[i] = int.Parse(fields[i]);
                }
                fem.tets_data.Add(values);
            }
        }

        //create tet objects
        GameObject parent = fem.gameObject;
        int id = 0;
        foreach (int[] values in fem.tets_data)
        {
            //get the tets verts indices
            int i0 = values[0];
            int i1 = values[1];
            int i2 = values[2];
            int i3 = values[3];

            Vector3 v0 = fem.verts_data[i0];
            Vector3 v1 = fem.verts_data[i1];
            Vector3 v2 = fem.verts_data[i2];
            Vector3 v3 = fem.verts_data[i3];

            GameObject go = new GameObject("tet_go (" + id++ + ")");
            Tetrahedron tet = go.AddComponent<Tetrahedron>();
            tet.verts = new List<Vector3> { v0, v1, v2, v3};
            fem.tets.Add(tet);

            go.transform.parent = parent.transform;
        }

        //cache min-max points of the mesh
        fem.minX = fem.verts_data.Min(v => v.x);
        fem.minY = fem.verts_data.Min(v => v.y);
        fem.minZ = fem.verts_data.Min(v => v.z);
        fem.maxX = fem.verts_data.Max(v => v.x);
        fem.maxY = fem.verts_data.Max(v => v.y);
        fem.maxZ = fem.verts_data.Max(v => v.z);

    }
}
