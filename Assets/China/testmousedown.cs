using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class testmousedown : MonoBehaviour{
	private float x = 0, y = 0;

	public float rotateSpeed = 5f;

	private Transform mTrans;

	private Vector3 distanceVec;

	public Transform player;
	public Vector3 linerSpeed;
	public Vector3 circleDot;
	
	private float radius;
	private Rigidbody body;
	private float speed;
	private float omga;
	// Use this for initialization
	void Start () {
		body=gameObject.GetComponent<Rigidbody> ();
		body.velocity = linerSpeed;
		radius = (circleDot - gameObject.transform.position).magnitude;
		speed = linerSpeed.magnitude;
		omga = speed *speed/ radius;
	}
	
	// Update is called once per frame
	void Update ()
	{
		changeforce();
	}
	void FixedUpdate () {
//		Vector3 fp = circleDot - gameObject.transform.position;//向心力矢量，但此时向量模不正确
//		fp = fp.normalized * body.mass * omga;//纠正向量的模
//		body.AddForce (fp,ForceMode.Force);
	}

	void changeforce()
	{
		for (float i = 0; i <= Mathf.PI * 2 ; i += (Mathf.PI * 2)/20)//相当于多添加了一组首坐标 ，这个坐标是绕了一圈后也就是2pi的相当于原点
		{
			Vector3 tpos=new Vector3(0,1*Mathf.Sin(i),1*Mathf.Cos(i));
			Vector3 mpos = transform.position;

			Vector3 forcePos = (tpos - mpos);
			body.AddForce(forcePos*10);
		}
	}

	private void MOVE()
	{
		x += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
		y -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
		//y = Mathf.Clamp(y, -60, 80);
		//根据人物位置确定摄像机的位置，这里用四元数进行了旋转 
		Quaternion camPosRotation = Quaternion.Euler(y, 0, 0);
		// mTrans.rotation = camPosRotation;
		mTrans.position= Vector3.Lerp(mTrans.position, (camPosRotation * distanceVec + player.position), Time.deltaTime*20f);
		//camPosRotation* distanceVec返回一个Vector3朝向你的四元数的方向headRotation。
		//得到一个朝向你所要旋转方向的一个向量
		//Debug.Log(camPosRotation * distanceVec);
		//mTrans.LookAt(player.position);
	}
}