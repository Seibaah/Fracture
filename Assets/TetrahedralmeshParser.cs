using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TetrahedralmeshParser
{
    /// <summary>
    /// Parses 2 files. One contains vertex coordinates and the other 
    /// contains tetrahedra vertex indices.
    /// </summary>
    /// <param name="verts_path">Path containing mesh vertex information</param>
    /// <param name="tets_path">Path of file containing a tetrahedra verte index set</param>
    /// <param name="rawVerts">List containing parsed vertex coordinates data</param>
    /// <param name="rawTets">List containing parsed tetrahedra index set data</param>
    public static void ParseTetMeshFiles(string verts_path, string tets_path, 
        out List<Vector3> rawVerts, out List<int[]> rawTets)
    {
        rawVerts = new List<Vector3>();
        rawTets = new List<int[]>();

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
                rawVerts.Add(new Vector3(values[0], values[1], values[2]));
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
                rawTets.Add(values);
            }
        }
    }
}
