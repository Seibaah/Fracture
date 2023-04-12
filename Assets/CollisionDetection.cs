using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UIElements;

public class CollisionDetection : MonoBehaviour
{
    List<BoundingBox> collisionClients = new List<BoundingBox>();
    List<IntervalPoint> xAxisIntervals = new List<IntervalPoint>();
    List<IntervalPoint> yAxisIntervals = new List<IntervalPoint>();
    List<IntervalPoint> zAxisIntervals = new List<IntervalPoint>();

    Dictionary<string, CollisionPair> potentialCollisions = new Dictionary<string, CollisionPair>();

    void Start()
    {
        Debug.Log("CCD START");
    }

    void Update()
    {
        if (collisionClients.Count is 0 or 1) return;

        SortIntervals(xAxisIntervals);
        SortIntervals(yAxisIntervals);
        SortIntervals(zAxisIntervals);

        //foreach(IntervalPoint pt in zAxisIntervals)
        //{
        //    Debug.Log(pt.GetValue());
        //}

        ScanIntervals(xAxisIntervals, Axis.X);
        ScanIntervals(yAxisIntervals, Axis.Y);
        ScanIntervals(zAxisIntervals, Axis.Z);

        //foreach(CollisionPair cp in potentialCollisions.Values)
        //{
        //    if (cp.xOverlap && cp.yOverlap && cp.zOverlap)
        //    {
        //        Debug.Log("True");
        //    }
        //    else
        //    {
        //        Debug.Log("False");
        //    }
        //}

        foreach (var pair in potentialCollisions.Where(pair => pair.Value.activeOnFrame == false).ToList())
        {
            potentialCollisions.Remove(pair.Key);
        }
        foreach (var pair in potentialCollisions)
        {
            pair.Value.activeOnFrame = false;
            pair.Value.xOverlap = false;
            pair.Value.yOverlap = false;
            pair.Value.zOverlap = false;
        }

        foreach (var pair in potentialCollisions)
        {
            pair.Value.box1.OnCollisionChildren(pair.Value.box2);
            pair.Value.box2.OnCollisionChildren(pair.Value.box1);
        }
    }

    void ScanIntervals(List<IntervalPoint> list, Axis ax)
    {
        List<BoundingBox> activeBoundingBoxes = new List<BoundingBox>();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].isMin) activeBoundingBoxes.Add(list[i].box);
            else
            {
                if (activeBoundingBoxes.Count is 0 or 1)
                {
                    activeBoundingBoxes.Remove(list[i].box);    
                    continue;
                }

                //creat collision pairs for all the bounding boxes in the set
                BoundingBox box1 = list[i].box;
                foreach (BoundingBox box2 in activeBoundingBoxes)
                {
                    if (box1.Equals(box2)) continue; //avoid making a collision pair with oneself

                    CollisionPair currentCollisionPair;
                    string key = string.Join("-", new[] { box1.id, box2.id }.OrderBy(id => id));

                    if (potentialCollisions.TryGetValue(key, out currentCollisionPair))
                    {
                        if (ax == Axis.X) currentCollisionPair.xOverlap = true; 
                        else if (ax == Axis.Y) currentCollisionPair.yOverlap = true;    
                        else currentCollisionPair.zOverlap = true; 

                        currentCollisionPair.activeOnFrame= true;
                    }
                    else
                    {
                        currentCollisionPair = new CollisionPair(box1, box2);

                        if (ax == Axis.X) currentCollisionPair.xOverlap = true;
                        else if (ax == Axis.Y) currentCollisionPair.yOverlap = true;
                        else currentCollisionPair.zOverlap = true;

                        currentCollisionPair.activeOnFrame= true;
                        potentialCollisions.Add(key, currentCollisionPair); 
                    }
                }
                activeBoundingBoxes.Remove(list[i].box);
            }
        }
    }

    /// <summary>
    /// Insertion sort of the intervals. O(n^2) avg case, but due to frame to frame spatial 
    /// consistency O(n) expected runtime after first pass
    /// </summary>
    /// <param name="list">The list of intervals min and max points to sort</param>
    void SortIntervals(List<IntervalPoint> list)
    {
        int j;
        for (int i = 1; i < list.Count; i++)
        {
            j = i;
            while (j > 0 && list[j-1].GetValue() > list[j].GetValue())
            {
                Swap(list, j, j - 1);
                j--;
            }
        }
    }

    // TODO might need to replace by adding a sorted insert
    public void AddCollisionClient(BoundingBox box)
    {
        collisionClients.Add(box);
        var max = box.bounds.max;
        var min = box.bounds.min;

        xAxisIntervals.Add(new IntervalPoint(PointType.Max_x, box, false));
        xAxisIntervals.Add(new IntervalPoint(PointType.Min_x, box, true));
        yAxisIntervals.Add(new IntervalPoint(PointType.Max_y, box, false));
        yAxisIntervals.Add(new IntervalPoint(PointType.Min_y, box, true));
        zAxisIntervals.Add(new IntervalPoint(PointType.Max_z, box, false));
        zAxisIntervals.Add(new IntervalPoint(PointType.Min_z, box, true));
    }

    public static void Swap<T>(List<T> list, int index1, int index2)
    {
        T temp = list[index1];
        list[index1] = list[index2];
        list[index2] = temp;
    }
}

public class IntervalPoint
{
    public PointType pointType;
    public BoundingBox box;
    public bool isMin;

    public IntervalPoint(PointType pt, BoundingBox b, bool isMin)
    {
        pointType = pt;
        box = b;
        this.isMin = isMin;
    }

    public float GetValue()
    {
        if (pointType == PointType.Max_x) return box.bounds.max.x;
        else if (pointType == PointType.Max_y) return box.bounds.max.y;
        else if (pointType == PointType.Max_z) return box.bounds.max.z;
        else if (pointType == PointType.Min_x) return box.bounds.min.x;
        else if (pointType == PointType.Min_y) return box.bounds.min.y;
        else return box.bounds.min.z;
    }
}

public class CollisionPair
{
    public BoundingBox box1;
    public BoundingBox box2;
    public bool xOverlap = false;
    public bool yOverlap = false;
    public bool zOverlap = false;
    public bool activeOnFrame = false;

    public CollisionPair(BoundingBox b1, BoundingBox b2)
    {
        box1 = b1;
        box2 = b2;
    }
}

public enum Axis
{
    X,
    Y,
    Z
}

public enum PointType
{
    Max_x,
    Max_y,
    Max_z,
    Min_x,
    Min_y,
    Min_z
}
