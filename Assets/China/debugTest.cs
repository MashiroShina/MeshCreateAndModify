using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debugTest : MonoBehaviour
{
	public float power=10;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		transform.Rotate(new Vector3(0,power,0));
	}
}
