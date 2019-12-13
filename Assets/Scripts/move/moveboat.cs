using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveboat : MonoBehaviour
{

    //公有类成员变量，将会显示在Unity的Inspector界面中
    public float walkSpeed = 2f;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(Random.Range(0, walkSpeed * 2) * Time.deltaTime, Random.Range(0, walkSpeed * 2) * Time.deltaTime, walkSpeed * Time.deltaTime);
    }

}
