using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 20.0f;
    private Camera cam;

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
    }
}
