﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundedCube : MonoBehaviour {

	public int xSize, ySize, zSize;
	public float roundness;
	private Vector3[] normals;
	
	private Mesh mesh;
	private Vector3[] vertices;

	private void Awake () {
		Generate();
	}

	private void Generate () {
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "Procedural Cube";
		CreateVertices();
		CreateTriangles();
	}

	private void CreateVertices () {
		int cornerVertices = 8;
		int edgeVertices = (xSize + ySize + zSize - 3) * 4;
		int faceVertices = (
			(xSize - 1) * (ySize - 1) +
			(xSize - 1) * (zSize - 1) +
			(ySize - 1) * (zSize - 1)) * 2;
		vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
		
		normals = new Vector3[vertices.Length];
		
		int v = 0;
		for (int y = 0; y <= ySize; y++) {
			for (int x = 0; x <= xSize; x++) {
				SetVertex(v++, x, y, 0);
			}
			for (int z = 1; z <= zSize; z++) {
				SetVertex(v++, xSize, y, z);
			}
			for (int x = xSize - 1; x >= 0; x--) {
				SetVertex(v++, x, y, zSize);
			}
			for (int z = zSize - 1; z > 0; z--) {
				SetVertex(v++, 0, y, z);
			}
		}
		for (int z = 1; z < zSize; z++) {
			for (int x = 1; x < xSize; x++) {
				SetVertex(v++, x, ySize, z);
			}
		}
		for (int z = 1; z < zSize; z++) {
			for (int x = 1; x < xSize; x++) {
//				vertices[v++] = new Vector3(x, 0, z);
				SetVertex(v++, x, 0, z);
			}
		}

		mesh.vertices = vertices;
		mesh.normals = normals;
		gameObject.AddComponent<BoxCollider>();
	}
	
	private void SetVertex (int i, int x, int y, int z) {
//		vertices[i] = new Vector3(x, y, z);
		Vector3 inner = vertices[i] = new Vector3(x, y, z);
		
		if (x < roundness) {
			inner.x = roundness;
		}
		else if (x > xSize - roundness) {
			inner.x = xSize - roundness;
		}
		if (y < roundness) {
			inner.y = roundness;
		}
		else if (y > ySize - roundness) {
			inner.y = ySize - roundness;
		}
		if (z < roundness) {
			inner.z = roundness;
		}
		else if (z > zSize - roundness) {
			inner.z = zSize - roundness;
		}
		//Debug.Log(inner+" "+(vertices[i]-inner));
		normals[i] = (vertices[i] - inner).normalized;//Init Normals =0,0,0
		//Debug.Log(normals[i]);
		vertices[i] = inner + normals[i] * roundness;
	}
	
	private void CreateTriangles () {
		int quads = (xSize * ySize + xSize * zSize + ySize * zSize) * 2;
		int[] triangles = new int[quads * 6];//一个quads有6个点所以要*6。一个三角形3个2个三角行成一个quad所以6个
		int ring = (xSize + zSize) * 2;//表示地面的最后一个定点绕了一圈 ring=14
		int t = 0, v = 0;
		for (int y = 0; y < ySize; y++,v++)
		{
			for (int q = 0; q < ring-1; q++, v++) {
				t = SetQuad(triangles, t, v, v + 1, v + ring, v + ring + 1);
			}
			//v=13
			t = SetQuad(triangles, t, v, v - ring + 1, v + ring, v + 1);
		}
		t = CreateTopFace(triangles, t, ring);
		t = CreateBottomFace(triangles, t, ring);
		mesh.triangles = triangles;
	}
	private int CreateTopFace (int[] triangles, int t, int ring) {
		int v = ring * ySize;
		for (int x = 0; x < xSize - 1; x++, v++) {
			t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
		}
		t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);
		//上面绘制顶部第一排
		int vMin = ring * (ySize + 1) - 1;
		int vMid = vMin + 1;
		int vMax = v + 2;
		for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++)
		{
			t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + xSize - 1);
			for (int x = 1; x < xSize - 1; x++, vMid++)
			{
				t = SetQuad(
					triangles, t,
					vMid, vMid + 1, vMid + xSize - 1, vMid + xSize);
			}

			t = SetQuad(triangles, t, vMid, vMax, vMid + xSize - 1, vMax + 1);
		}

		//上面是绘制顶部中间两排
		int vTop = vMin - 2;
		t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
		for (int x = 1; x < xSize - 1; x++, vTop--, vMid++) {
			t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
		}
		t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);
		//上面是绘制顶部最后一排
		return t;
	}
	private int CreateBottomFace (int[] triangles, int t, int ring) {
		int v = 1;
		int vMid = vertices.Length - (xSize - 1) * (zSize - 1);
		t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
		for (int x = 1; x < xSize - 1; x++, v++, vMid++) {
			t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
		}
		t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

		int vMin = ring - 2;
		vMid -= xSize - 2;
		int vMax = v + 2;

		for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++) {
			t = SetQuad(triangles, t, vMin, vMid + xSize - 1, vMin + 1, vMid);
			for (int x = 1; x < xSize - 1; x++, vMid++) {
				t = SetQuad(
					triangles, t,
					vMid + xSize - 1, vMid + xSize, vMid, vMid + 1);
			}
			t = SetQuad(triangles, t, vMid + xSize - 1, vMax + 1, vMid, vMax);
		}

		int vTop = vMin - 1;
		t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
		for (int x = 1; x < xSize - 1; x++, vTop--, vMid++) {
			t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
		}
		t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);
		
		return t;
	}

	

	private static int
	SetQuad (int[] triangles, int i, int v00, int v10, int v01, int v11) {
		/*
		*   v01     v11
		* 	 1/4	  5
		*	   *
		*       *
		* 		 *
		* 		  *
		* 		   *
		* 	        *
		* 	   0	2/3
		*   v00 	v10
		*/
		triangles[i] = v00;
		triangles[i + 1] = triangles[i + 4] = v01;
		triangles[i + 2] = triangles[i + 3] = v10;
		triangles[i + 5] = v11;
		return i + 6;
	}

	private void OnDrawGizmos () {
		if (vertices == null) {
			return;
		}
		Gizmos.color = Color.black;
		for (int i = 0; i < vertices.Length; i++) {
			Gizmos.color=Color.black;
			Gizmos.DrawSphere(vertices[i], 0.1f);
			Gizmos.color=Color.yellow;
			Gizmos.DrawRay(vertices[i],normals[i]);
		}
	}
}
