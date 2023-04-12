using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class BoundingBoxTree
{
    public static void VerticalSplit(BoundingBox box, int level)
    {
        Assert.IsTrue(box != null);

        if (level == 0) return; 

        var min = box.bounds.min;
        var max = box.bounds.max;
        var center = box.bounds.center;

        box.leftChild = new BoundingBox(min.x, min.y, min.z, 
            center.x, max.y, max.z);

        box.rightChild = new BoundingBox(center.x, min.y, min.z,
            max.x, max.y, max.z);
        box.hasChildren = true;

        HorizontalSplit(box.leftChild, level - 1);
        HorizontalSplit(box.rightChild, level - 1); 
    }

    public static void HorizontalSplit(BoundingBox box, int level)
    {
        Assert.IsTrue(box != null);

        if (level == 0) return;

        var min = box.bounds.min;
        var max = box.bounds.max;
        var center = box.bounds.center;

        box.leftChild = new BoundingBox(min.x, min.y, min.z,
            max.x, center.y, max.z);

        box.rightChild = new BoundingBox(min.x, center.y, min.z,
            max.x, max.y, max.z);
        box.hasChildren = true;

        VerticalSplit(box.leftChild, level - 1);
        VerticalSplit(box.rightChild, level - 1);
    }

    public static void Draw(BoundingBox box)
    {
        box.Draw();

        if (box.leftChild is not null) Draw(box.leftChild);
        if (box.rightChild is not null) Draw(box.rightChild);
    }
}

public class BoundingBox
{
    static int ID = 0;

    public int id;
    public bool isRoot = false;
    public bool hasChildren = false;

    public Bounds bounds;
    public BoundingBox leftChild, rightChild;

    public BoundingBox(float minX, float minY, float minZ,
        float maxX, float maxY, float maxZ)
    {
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        bounds = new Bounds(center, size);
        id = ID++;
    }

    public BoundingBox(Bounds b) 
    {
        bounds = b; 
        id = ID++;
    }

    public void OnCollisionChildren(BoundingBox b2)
    {
        if (leftChild is null && rightChild is null) return;

        if (leftChild.bounds.Intersects(b2.bounds)) {
            leftChild.OnCollisionChildren(b2);
        }
        else if (rightChild.bounds.Intersects(b2.bounds))
        {
            rightChild.OnCollisionChildren(b2);
        }
    }

    public void Draw()
    {
        ////todo only spawn cube if no children
        //if (hasChildren is true) {
        //    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    cube.transform.position = bounds.center;
        //    cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //}

        Vector3 v3FrontTopLeft = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z);  // Front top left corner
        Vector3 v3FrontTopRight = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z);  // Front top right corner
        Vector3 v3FrontBottomLeft = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z);  // Front bottom left corner
        Vector3 v3FrontBottomRight = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z);  // Front bottom right corner
        Vector3 v3BackTopLeft = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z);  // Back top left corner
        Vector3 v3BackTopRight = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z);  // Back top right corner
        Vector3 v3BackBottomLeft = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z);  // Back bottom left corner
        Vector3 v3BackBottomRight = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z);  // Back bottom right corner

        // Draw the front of the box
        Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, Color.green);
        Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, Color.green);
        Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, Color.green);
        Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, Color.green);

        // Draw the back of the box
        Debug.DrawLine(v3BackTopLeft, v3BackTopRight, Color.green);
        Debug.DrawLine(v3BackTopRight, v3BackBottomRight, Color.green);
        Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, Color.green);
        Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, Color.green);

        // Draw the connecting lines
        Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, Color.green);
        Debug.DrawLine(v3FrontTopRight, v3BackTopRight, Color.green);
        Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, Color.green);
        Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, Color.green);
    }
}
