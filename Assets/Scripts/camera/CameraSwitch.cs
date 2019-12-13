using System.Collections.Generic;
using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    public List<Camera> Cameras;

    private void Start()
    {
        EnableCamera(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EnableCamera(0);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            EnableCamera(1);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            EnableCamera(2);
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            EnableCamera(3);
        }

        /*
         * If you want to add more cameras, you need to add
         * some more 'else if' conditions just like above
         */
    }

    private void EnableCamera(int n)
    {
        Cameras.ForEach(cam => cam.enabled = false);
        Cameras[n].enabled = true;
    }
}