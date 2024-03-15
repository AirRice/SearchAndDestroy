using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public float speed = 15f;
    public float edgesize = 10f;
    public float scrollSensitivity = 10f;
    public float verticalInput;
    public float horizontalInput;

    public Vector3 defaultPos;
    public float defaultCamHei;
    
    // Start is called before the first frame update
    void Start()
    {
        defaultPos = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            Debug.Log("mwheel pressed");
            transform.position = defaultPos;
        }
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");


        /*float mousePosX = Input.mousePosition.x;
        float mousePosY = Input.mousePosition.y;

        if (mousePosX > Screen.width - edgesize){
            horizontalInput = 1f;
        }
        if (mousePosX < edgesize){
            horizontalInput = -1f;
        }
        if (mousePosY > Screen.height - edgesize){
            verticalInput = 1f;
        }
        if (mousePosY < edgesize){
            verticalInput = -1f;
        }       
        */
        transform.Translate(speed * verticalInput * Time.deltaTime * Vector3.up);
        transform.Translate(speed * horizontalInput * Time.deltaTime * Vector3.right);
    }
}
