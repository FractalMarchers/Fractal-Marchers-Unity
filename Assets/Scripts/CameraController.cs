using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 20.0f;
    public float rotationSpeed = 2f;
    private Camera cam;
    private Vector3 startPoint;
    private void Start()
    {
        cam = this.GetComponent<Camera>();
    }
    // Update is called once per frame
    void Update()
    {
        // TODO: Add camera movement

        
        Vector2 motion = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        this.transform.Translate(new Vector3(motion.x, 0.0f, motion.y) * speed * Time.deltaTime);

        if (Input.GetMouseButton(1))
        {
            startPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
            var targetRotation = Quaternion.LookRotation(startPoint - transform.position);

            var delta = transform.rotation * Quaternion.Inverse(targetRotation);
            Debug.Log(delta);

            // Smoothly rotate towards the target point.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
