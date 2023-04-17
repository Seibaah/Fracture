using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);

        foreach (ContactPoint contact in contacts)
        {
            var tet = contact.otherCollider.gameObject.GetComponent<Tetrahedron>();
            if (tet is not null)
            {
                if (tet.collisionDetected == false)
                {
                    tet.collisionDetected = true;
                    //Debug.Log(contact.otherCollider.gameObject.transform.name + " hit\n" +
                    //    "Separation Distance: " + contact.separation);
                    tet.ApplyCollisionForceToNodes(collision.impulse);
                    Destroy(gameObject, 2);
                }
            }
        }
    }
}
