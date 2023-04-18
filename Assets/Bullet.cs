using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float impactForce;

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);

        foreach (ContactPoint contact in contacts)
        {
            var tet = contact.otherCollider.gameObject.GetComponent<Tetrahedron>();
            if (tet is not null)
            {
                if (tet.collisionCount > 0)
                {
                    tet.collisionCount--;
                    //Debug.Log(contact.otherCollider.gameObject.transform.name + " hit\n" +
                    //    "Separation Distance: " + contact.separation);
                    tet.ApplyCollisionForceToNodes(contact.normal * impactForce);
                    tet.parentFemMesh.GetComponent<Rigidbody>().AddForce(contact.normal * impactForce);
                }
            }
        }
    }

    void Start()
    {
        Destroy(gameObject, 2);
    }
}
