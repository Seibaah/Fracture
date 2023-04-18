using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public float shootingForce = 1000f; // the force applied to the sphere when it's shot

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) ShootSphere();
    }

    private void ShootSphere()
    {
        // create a new sphere game object with a collider
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        SphereCollider sphereCollider = sphere.AddComponent<SphereCollider>();

        // add a rigidbody component to the sphere game object
        Rigidbody sphereRigidbody = sphere.AddComponent<Rigidbody>();
        sphereRigidbody.mass = 25f;

        // apply the shooting force to the sphere in the forward direction of the shooter
        sphereRigidbody.AddForce(Camera.main.ScreenPointToRay(Input.mousePosition).direction.normalized * shootingForce, ForceMode.Impulse);

        var bullet = sphere.AddComponent<Bullet>();
        bullet.impactForce = shootingForce;
    }
}
