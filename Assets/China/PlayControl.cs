using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayControl : MonoBehaviour {
//	public GameObject pottery;
//
//	public float Move_x;
//
//	public float Move_y;

	public int details = 40;
	public int layer =20;
	public float Height = 0.1f;

	public float OuterRadius = 1.0f;
	public float InnerRadius = 0.9f;

	public float changeBrushsize=5f;
	public bool canRotate = true;

	public Texture2D tex;//_BumpMap _MainTex _DetailNormalMap _DetailAlbedoMap
	public Camera cam2;
	public TextureWrapMode TextureWrapModes=TextureWrapMode.Clamp;
	private void Start()
	{
		
	}

	void Update()
	{
		RenderGoTex(TextureWrapModes);
	}
	void RenderGoTex(Enum warpMode)
	{
		if (Input.GetKeyDown(KeyCode.K))
		{
			if (tex!=null)
			{
				DestroyImmediate(tex);
			}
			cam2.gameObject.SetActive(true);
			RenderTexture rt = cam2.targetTexture;
			cam2.Render();
			RenderTexture.active = rt;
			//Debug.Log(RenderTexture.active);
			tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();
			tex.wrapMode = (TextureWrapMode)warpMode;// TextureWrapMode.Clamp;
			GetComponent<Renderer>().material.SetTexture("_MainTex",tex);
			cam2.gameObject.SetActive(false);
		}
	}
	public void setRotate()
	{
		if (canRotate)
		{
			transform.Rotate(new Vector3(0,1,0)*10f);
		}
	}
	public int setDetail()
	{
		return details;
	}
	public int setLayer()
	{
		return layer;
	}
	public float setHeight_of_each_floor()
	{
		return Height;
	}
	public float setOuterRadius()
	{
		return OuterRadius;
	}
	public float setInnerRadius()
	{
		return InnerRadius;
	}
	public float setchangeBrushsize()
	{
		return changeBrushsize;
	}
	public	Vector3 getRayPosition()
	{
		return Input.mousePosition;
	}
	public	bool useMouse(int leftOrRight=0)
	{
		return Input.GetMouseButton(leftOrRight);
	}
	public	float useWhell()
	{
		return Input.GetAxis("Mouse ScrollWheel");
	}
//	public	void GetMouseMove()
//	{
//		Move_x = Input.mousePosition.x - Camera.main.WorldToScreenPoint(pottery.transform.position).x;
//		//Debug.Log(Move_x);
//		Move_y = Input.mousePosition.y - Camera.main.WorldToScreenPoint(pottery.transform.position).y;
//	} 
}
