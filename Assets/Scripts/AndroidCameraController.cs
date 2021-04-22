using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidCameraController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {


                //Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                //touchPosition.z = 5f;
                //transform.position = touchPosition;


                Vector3 screenPosition = touch.position;
                screenPosition.z = 5f; // Camera.main.transform.position.y - transform.position.y;

                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                transform.position = worldPosition;

                RaycastHit hit;
                if (Physics.Raycast(new Vector3(worldPosition.x, worldPosition.y, 0f), Camera.main.transform.forward, out hit, 10000f))
                {
                    Debug.Log(hit.transform.name);
                }
                Debug.DrawRay(Camera.main.transform.forward, new Vector3(worldPosition.x, worldPosition.y, 0f), Color.red, 100f);
            }
        }
    }
}
