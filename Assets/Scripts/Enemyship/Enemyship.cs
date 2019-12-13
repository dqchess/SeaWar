using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemyship : MonoBehaviour {
    public float speed = 10f;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
       transform.position += new Vector3(Random.Range(0, speed * 2) * Time.deltaTime, Random.Range(0, speed * 2) * Time.deltaTime, speed * Time.deltaTime);
       //随机运动，产生随机方向和速度   
	}
}
