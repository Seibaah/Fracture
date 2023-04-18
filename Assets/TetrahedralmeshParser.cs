using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TetrahedralmeshParser
{
    public static void ParseTetMeshFiles(FemMesh fem, string verts_path, string tets_path)
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
    }
}
