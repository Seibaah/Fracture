//#define DEBUG_MODE_ON

using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float impactForce;
    public float mass;

    void Start()
    {
        Destroy(gameObject, 2); //bulets despawn 2 seconds after instantiation
    }

    /// <summary>
    /// Called when the bullet collides with another object.
    /// If the object is a tetrahedra we apply the collision force to its nodes
    /// </summary>
    /// <param name="collision">Contains collision event information</param>
    private void OnCollisionEnter(Collision collision)
    {
        var contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);

        foreach (ContactPoint contact in contacts)
        {
            var tet = contact.otherCollider.gameObject.GetComponent<Tetrahedron>();
            if (tet is not null)
            {
#if DEBUG_MODE_ON
                Debug.Log(contact.otherCollider.gameObject.transform.name + " hit\n" +
                    "Separation Distance: " + contact.separation);
#endif
                tet.ApplyCollisionForceToNodes(contact.point, contact.normal * impactForce);
            }
        }
    }
}
