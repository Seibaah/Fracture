using System;
using System.Collections;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Configuration")]
    public bool autoModeOn = false;
    public FiringMode firingMode = FiringMode.Single;

    [Header("Projectile Parameters")]
    public float shootingForce = 1000f; // the force applied to the sphere when it's shot
    public float projectileMass = 25f;
    public float projectileSize = 0.4f;

    [Header("Recorder")]
    public bool recordSession = false;
    RecorderWindow recWindow;

    void Start()
    {
        if (autoModeOn)
        {
            if (recordSession)
            {
                recWindow = (RecorderWindow)EditorWindow.GetWindow(typeof(RecorderWindow));
                recWindow.StartRecording();
            }
            StartCoroutine(FireGun());
        }
    }

    void Update()
    {
        if (!autoModeOn && Input.GetMouseButtonDown(0)) PlayerShootSphere();
    }

    /// <summary>
    /// Creates a spherical projectile shot in the mouse direction
    /// </summary>
    void PlayerShootSphere()
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

    /// <summary>
    /// Creates a spherical projectile 
    /// </summary>
    void ShootSphere()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = gameObject.transform.position;
        sphere.transform.localScale = new Vector3(1, 1, 1) * projectileSize;

        sphere.AddComponent<SphereCollider>();
        var sphereRigidbody = sphere.AddComponent<Rigidbody>();
        sphereRigidbody.mass = projectileMass;
        sphereRigidbody.AddForce(transform.forward.normalized * shootingForce, ForceMode.Impulse);

        var bullet = sphere.AddComponent<Bullet>();
        bullet.impactForce = shootingForce;
        bullet.mass = projectileMass;
    }

    /// <summary>
    /// Coroutine that runs the auto shooter
    /// </summary>
    /// <returns></returns>
    public IEnumerator FireGun()
    {
        if (firingMode == FiringMode.Single)
        {
            yield return new WaitForSeconds(2.15f);
            ShootSphere();
        }
        else
        {
            yield return new WaitForSecondsRealtime(4.15f);
            ShootSphere();
            yield return new WaitForSecondsRealtime(0.15f);
            ShootSphere();
            yield return new WaitForSecondsRealtime(0.15f);
            ShootSphere();
        }

        if (recordSession)
        {
            yield return new WaitForSeconds(3f);
            recWindow.StopRecording();
        }
    }
}

public enum FiringMode
{
    Single,
    Burst
} 
