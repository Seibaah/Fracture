using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Projectile Parameters")]
    public float shootingForce = 1000f; // the force applied to the sphere when it's shot
    public float projectileMass = 25f;
    public float projectileSize = 0.4f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) ShootSphere();
    }

    /// <summary>
    /// Creates a spherical projectile shot in the mouse direction
    /// </summary>
    void ShootSphere()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        sphere.transform.localScale = new Vector3(1, 1, 1) * projectileSize;

        sphere.AddComponent<SphereCollider>();
        var sphereRigidbody = sphere.AddComponent<Rigidbody>();
        sphereRigidbody.mass = projectileMass;
        sphereRigidbody.AddForce(Camera.main.ScreenPointToRay(Input.mousePosition).direction.normalized * shootingForce, ForceMode.Impulse);

        var bullet = sphere.AddComponent<Bullet>();
        bullet.impactForce = shootingForce;
        bullet.mass = projectileMass;
    }
}
