using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileLaunch : MonoBehaviour {
    public float speed = 12f;
    public float m_RotSpeed = 60f;
    public GameObject m_Explosion;
    public Transform Target;
    public float acceleration = 20f;
	// Use this for initialization
	// Update is called once per frame

    void Update () {
        Vector3 target = (Target.position - transform.position).normalized;
        float a = Vector3.Angle(transform.forward, target) / m_RotSpeed;
        if (a > 0.1f || a < -0.1f)
            transform.forward = Vector3.Slerp(transform.forward, target, Time.deltaTime / a).normalized;
        else
        {
            speed += acceleration * Time.deltaTime;
            transform.forward = Vector3.Slerp(transform.forward, target, 1).normalized;
        }
        transform.position += transform.forward * speed * Time.deltaTime;
	} 

   
    private void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag=="Player")
        {
            DestroyImmediate(gameObject);
            DestroyImmediate(other.gameObject);
            Destroy(Instantiate(m_Explosion, transform.position, Quaternion.identity), 3f);
        }
        
    }
}
