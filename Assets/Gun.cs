using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Configuration")]
    public bool autoModeOn = false;
    public FiringMode firingMode = FiringMode.Single;

    [Header("Projectile Parameters")]
    public float shootingForce = 1000f;
    public float projectileMass = 25f;
    public float projectileSize = 0.4f;

    [Header("Recorder")]
    public bool recordSession = false;
    RecorderWindow recWindow;

    Vector3 unitVector = new Vector3(1, 1, 1);

    //Object Pooling for projectiles
    int poolSize = 3;
    Vector3 poolLocation = new Vector3(0f, 0f, -25f);
    GameObject poolParent;
    List<GameObject> pool = new List<GameObject>();

    void Start()
    {
        //create an object pool of bullets
        poolParent = new GameObject("Projectile Pool");
        poolParent.transform.position = poolLocation;
        for (int i = 0; i < poolSize; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = poolParent.transform;
            sphere.transform.position = poolLocation;
            sphere.AddComponent<SphereCollider>();
            var rb = sphere.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var bullet = sphere.AddComponent<Bullet>();
            bullet.enabled = false;
            bullet.gun = this;
            pool.Add(sphere);

            Destroy(sphere, 8f);
        }

        if (recordSession)
        {
            recWindow = (RecorderWindow)EditorWindow.GetWindow(typeof(RecorderWindow));
            recWindow.StartRecording();
        }

        if (autoModeOn)
        {
            StartCoroutine(FireGun());
        }
    }

    void Update()
    {
        if (!autoModeOn && Input.GetMouseButtonDown(0)) PlayerShootSphere();
    }

    /// <summary>
    /// Player shoots a bullet
    /// </summary>
    void PlayerShootSphere()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        sphere.transform.localScale = unitVector * projectileSize;

        sphere.AddComponent<SphereCollider>();
        var sphereRigidbody = sphere.AddComponent<Rigidbody>();
        sphereRigidbody.mass = projectileMass;
        sphereRigidbody.AddForce(Camera.main.ScreenPointToRay(Input.mousePosition).direction.normalized * shootingForce, ForceMode.Impulse);

        var bullet = sphere.AddComponent<Bullet>();
        bullet.impactForce = shootingForce;
        bullet.mass = projectileMass;

        Destroy(sphere, 2f);
    }

    /// <summary>
    /// Shoot a bullet
    /// </summary>
    void AutoShootSphere()
    {
        var sphere = pool[0];
        pool.RemoveAt(0);
        sphere.transform.position = gameObject.transform.position;
        sphere.transform.localScale = unitVector * projectileSize;

        var sphereRigidbody = sphere.GetComponent<Rigidbody>();
        sphereRigidbody.isKinematic = false;
        sphereRigidbody.mass = projectileMass;
        sphereRigidbody.AddForce(transform.forward.normalized * shootingForce, ForceMode.Impulse);

        var bullet = sphere.GetComponent<Bullet>();
        bullet.impactForce = shootingForce;
        bullet.mass = projectileMass;
    }

    /// <summary>
    /// Coroutine that shoots bullets at certain intervals depending on firing mode
    /// </summary>
    /// <returns></returns>
    public IEnumerator FireGun()
    {
        if (firingMode == FiringMode.Single)
        {
            yield return new WaitForSeconds(2.15f);
            AutoShootSphere();
        }
        else
        {
            yield return new WaitForSecondsRealtime(4.15f);
            AutoShootSphere();
            yield return new WaitForSecondsRealtime(0.25f);
            AutoShootSphere();
            yield return new WaitForSecondsRealtime(0.25f);
            AutoShootSphere();
        }

        if (recordSession)
        {
            yield return new WaitForSeconds(10f);
            recWindow.StopRecording();
        }
    }
}

public enum FiringMode
{
    Single,
    Burst
} 
