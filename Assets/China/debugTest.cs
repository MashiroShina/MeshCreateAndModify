using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debugTest : MonoBehaviour
{
	public float power=10;

	public bool openOpwer = true;

	public GameObject obj;

	private Mesh mMesh;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (openOpwer)
		{
			transform.Rotate(new Vector3(0,power,0));
		}
		drag();
	}

	public void drag()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		RaycastHit hit = new RaycastHit();

		if (Physics.Raycast(ray, out hit))

		{

			if (hit.collider.name == "Capsule")

			{

				transform.position =new Vector3(hit.point.x,hit.point.z,hit.point.z);

			}

		}
	}
}
