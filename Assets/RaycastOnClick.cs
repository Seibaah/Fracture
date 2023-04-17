using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastOnClick : MonoBehaviour
{
    void Update()
    {
        // Check if the left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            // Calculate the raycast from the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast and print the hit information to the console
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                Debug.Log("Hit object: " + hitInfo.collider.gameObject.name);
                Debug.Log("Hit wcf point: " + hitInfo.point);
                Debug.Log("Hit normal: " + hitInfo.normal);
                Debug.Log("Barycentric coords" + hitInfo.barycentricCoordinate);

                var go = hitInfo.collider.gameObject;
                var tet = go.GetComponent<Tetrahedron>();
            }
        }
    }
}
