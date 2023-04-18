using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner: ScriptableObject
{
    /// <summary>
    /// Spawns a wall of bricks gameobjects tailored for demo1
    /// </summary>
    /// <param name="parent">Parent object of the bricks</param>
    /// <param name="brickPrefabs">List of prefabs bricks to use</param>
    /// <returns></returns>
    public List<GameObject> SpawnBricksDemo_1(GameObject parent, List<GameObject> brickPrefabs)
    {
        var instantiatedBricks = new List<GameObject>();

        for (int z = 0; z < 2; z++)
        {
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    GameObject go = GameObject.Instantiate(brickPrefabs[(x+y+z)%brickPrefabs.Count], new Vector3(-1.25f + x * 0.5f, -1.33f + y * 0.33f, -0.25f + z * 0.5f), Quaternion.Euler(270f, 0f, 0f));
                    go.transform.parent = parent.transform;
                    instantiatedBricks.Add(go);
                }
            }
        }

        return instantiatedBricks;
    }
}
