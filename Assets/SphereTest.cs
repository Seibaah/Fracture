using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereTest : MonoBehaviour
{
    public BoundingBox bb;
    SphereCollider collider2;

    void Start()
    {
        collider2 = gameObject.GetComponent<SphereCollider>();
        var bounds = collider2.bounds;
        bb = new BoundingBox(bounds);
        
            //create a collideable item of the rootbox
        var ccd = GameObject.Find("CCD").GetComponent<CollisionDetection>();
        ccd.AddCollisionClient(bb);
    }

    void Update()
    {
        bb.bounds= collider2.bounds;

        gameObject.transform.position += new Vector3(0.0f, 0.0f, -1.0f) * Time.deltaTime;
    }
}
