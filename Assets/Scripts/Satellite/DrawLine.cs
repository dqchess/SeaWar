using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLine : MonoBehaviour {

    [SerializeField]
    private Transform satellite;
    [SerializeField]
    private Transform station;
    [SerializeField]
    private LineRenderer line;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        line.SetPosition(0, satellite.position);
        line.SetPosition(1, station.position);
	}
}
