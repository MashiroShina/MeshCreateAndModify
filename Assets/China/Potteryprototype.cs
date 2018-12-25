using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Potteryprototype : MonoBehaviour
{

	private PlayControl _playControl;
	
	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	MeshCollider meshCollider;
	Mesh mesh;
	
	private int details = 40;
	private int layer =20;
	private float Height = 0.1f;

	private float OuterRadius = 1.0f;
	private float InnerRadius = 0.9f;

	private float Smooth=5f;
	
	List<Vector3> vertices;
	List<Vector2> UV;
	List<int> triangles;

	float EachAngle ;
	int SideCount;

	void init()
	{
		_playControl = GetComponent<PlayControl>();
		setAttributes();
	}

	private void setAttributes()
	{
		details = _playControl.setDetail();
		layer = _playControl.setLayer();
		Height = _playControl.setHeight_of_each_floor();
		OuterRadius = _playControl.setOuterRadius();
		InnerRadius = _playControl.setInnerRadius();
		Smooth = _playControl.setchangeBrushsize();
	}

	// Use this for initialization
	void Start ()
	{
		init();
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();
		meshRenderer = GetComponent<MeshRenderer>();
		GeneratePrototype();
		//NormalsReBuild();
		//StartCoroutine(SequenceTest());
	}
	
	// Update is called once per frame
	void Update ()
	{
		Smooth = _playControl.setchangeBrushsize();
		_playControl.setRotate();
		GetMouseControlTransform();
		//Debug.Log(points);
	}

	[ContextMenu("GeneratePottery")]
	void GeneratePrototype()
	{
		setAttributes();
		vertices = new List<Vector3>();
		triangles = new List<int>();
		UV = new List<Vector2>();
		
		EachAngle = Mathf.PI * 2 / details;//细分程度
		for (int i = 0; i < layer; i++)
		{
			GenerateCircle(i);
		}
		Capping();
        
		mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = UV.ToArray();

		mesh.RecalculateBounds();
		mesh.RecalculateTangents();

		meshFilter.mesh = mesh;
		mesh.RecalculateNormals();
		meshCollider.sharedMesh = mesh;
	}

	void GenerateCircle(int _layer)//_layer控制当前在绘制第几层!!!，layer表示要绘制多少层然后一层层遍历
	{
		//外顶点与内顶点分开存储,方便变化操作时的计算
		List<Vector3> vertices_outside = new List<Vector3>();
		List<Vector3> vertices_inside = new List<Vector3>();

       
		List<Vector2> UV_outside = new List<Vector2>();
		List<Vector2> UV_inside = new List<Vector2>();
		
		//外侧和内侧顶点计算,_layer=0->_layer=20
		//注意这里让每一圈的首尾重合了,也就是开始和结尾的顶点坐标一致
		//目的是计算UV坐标时不会出现空缺
		for (float i = 0; i <= Mathf.PI * 2 + EachAngle; i += EachAngle)//相当于多添加了一组首坐标 ，这个坐标是绕了一圈后也就是2pi的相当于原点
		{
			Vector3 v1 = new Vector3(OuterRadius * Mathf.Sin(i), _layer * Height, OuterRadius * Mathf.Cos(i));
			Vector3 v2 = new Vector3(OuterRadius * Mathf.Sin(i), (_layer + 1) * Height, OuterRadius * Mathf.Cos(i));

			Vector3 v3 = new Vector3(InnerRadius * Mathf.Sin(i), _layer * Height, InnerRadius * Mathf.Cos(i));
			Vector3 v4 = new Vector3(InnerRadius * Mathf.Sin(i), (_layer + 1) * Height, InnerRadius * Mathf.Cos(i));
			//存放定点坐标,这里额外列出两个内外顶点坐标是为了我们方便后续对其的管理
			vertices_outside.Add(v1);
			vertices_outside.Add(v2);

			vertices_inside.Add(v3);
			vertices_inside.Add(v4);

			Vector2 uv1 = new Vector2(i / Mathf.PI * 2, _layer * 1.0f / layer * 1.0f);
			Vector2 uv2 = new Vector2(i / Mathf.PI * 2, (_layer + 1) * 1.0f / layer * 1.0f);
			Vector2 uv3 = new Vector2(i / Mathf.PI * 2, _layer * 1.0f / layer * 1.0f);
			Vector2 uv4 = new Vector2(i / Mathf.PI * 2, (_layer + 1) * 1.0f / layer * 1.0f);
			
			UV_outside.Add(uv1); 
			UV_outside.Add(uv2);
			
			UV_inside.Add(uv3); 
			UV_inside.Add(uv4);
		}

		//添加外内顶点坐标
		vertices.AddRange(vertices_outside);
		vertices.AddRange(vertices_inside);

		UV.AddRange(UV_outside);
		UV.AddRange(UV_inside);

		SideCount = vertices_outside.Count;
		int j = vertices_outside.Count * _layer * 2;//当前层数
		int n = vertices_outside.Count;

			for (int i =j; i < j+vertices_outside.Count-2; i+=2)//i=j相当于更新到下一层
			{
				//绘制外圈
				triangles.Add(i); 
				triangles.Add(i + 2);
				triangles.Add(i + 1);
				
				triangles.Add(i + 2); 
				triangles.Add(i + 3); 
				triangles.Add(i + 1);
				
				//绘制内圈,反向绘制让其对我们可见
				triangles.Add(i + n); 
				triangles.Add(i + n + 1); 
				triangles.Add(i + n + 2);
				
				triangles.Add(i + n + 2); 
				triangles.Add(i + n + 1); 
				triangles.Add(i + n + 3);    
			}
	}
	//封顶
	void Capping()
	{
		for (float i = 0; i <= Mathf.PI * 2+EachAngle; i += EachAngle)
		{
			Vector3 outer = new Vector3(OuterRadius * Mathf.Sin(i),layer * Height, OuterRadius * Mathf.Cos(i));
			Vector3 inner= new Vector3(InnerRadius * Mathf.Sin(i), layer * Height, InnerRadius * Mathf.Cos(i));

			vertices.Add(outer);
			vertices.Add(inner);

			Vector2 uv1 = new Vector2(i / Mathf.PI * 2,0); 
			Vector2 uv2 = new Vector2(i / Mathf.PI * 2, 1);
            
			UV.Add(uv1); 
			UV.Add(uv2);
		}
		int j = SideCount * layer * 2;
		for (int i=j;i<vertices.Count-2;i+=2)
		{
			triangles.Add(i);
			triangles.Add(i + 3);
			triangles.Add(i + 1);
			
			triangles.Add(i);
			triangles.Add(i + 2);
			triangles.Add(i + 3);
		}
		//最后一个面
		triangles.Add(vertices.Count - 2);
		triangles.Add(j + 1);
		triangles.Add(vertices.Count - 1);
		
		triangles.Add(vertices.Count - 2);
		triangles.Add(j);
		triangles.Add(j + 1);
	}
	IEnumerator SequenceTest()
	{
		
		for (int i = 0; i < meshFilter.mesh.vertices.Length; i++)
		{    
			if (i % 2 == 0)
			{
				Debug.DrawRay(transform.TransformPoint(meshFilter.mesh.vertices[i]), transform.TransformDirection(meshFilter.mesh.normals[i] * 0.3f), Color.green, 1000f);
			}
			else
			{
				Debug.DrawRay(transform.TransformPoint(meshFilter.mesh.vertices[i]), transform.TransformDirection(meshFilter.mesh.normals[i] * 0.3f), Color.blue, 1000f);
			}

			yield return new WaitForSeconds(Time.deltaTime);

		}
		for (int i = 0; i < meshFilter.mesh.triangles.Length; i += 3)
		{          
			Debug.DrawLine(meshFilter.mesh.vertices[meshFilter.mesh.triangles[i]], meshFilter.mesh.vertices[meshFilter.mesh.triangles[i + 1]], Color.red, 100f);

			yield return new WaitForSeconds(0.01f);
			Debug.DrawLine(meshFilter.mesh.vertices[meshFilter.mesh.triangles[i + 1]], meshFilter.mesh.vertices[meshFilter.mesh.triangles[i + 2]], Color.yellow, 100f);

			yield return new WaitForSeconds(0.01f);
			Debug.DrawLine(meshFilter.mesh.vertices[meshFilter.mesh.triangles[i + 2]], meshFilter.mesh.vertices[meshFilter.mesh.triangles[i]], Color.blue, 100f);

			yield return new WaitForSeconds(0.01f);
			

		}
		
	}
	
	void GetMouseControlTransform()
	{
		Ray ray = Camera.main.ScreenPointToRay(_playControl.getRayPosition());
		RaycastHit info;
		if (Physics.Raycast(ray.origin, ray.direction, out info))
		{
			//Debug.Log(info.point.y);
			Mesh mesh = meshFilter.mesh;
			Vector3[] _vertices = mesh.vertices;
			for (int i = 0; i < _vertices.Length; i++)
			{
				//x,z平面变换
				//顶点移动与Y值的关系限制在5倍单层高度//Smooth=5
				//限制高度越大,曲线越平滑
				
				if (Mathf.Abs(info.point.y - transform.TransformPoint(_vertices[i]).y) < (Smooth * Height))
				{
					
					//计算顶点移动方向的向量,向内或外 因为x和z不变我们只要知道y的高度便可知道
					//Vector3 v_xz = (transform.TransformPoint(_vertices[i]) - 
					//              new Vector3(transform.position.x, transform.TransformPoint(_vertices[i]).y, transform.position.z));
					Vector3 v_xz = transform.TransformDirection(transform.InverseTransformPoint(_vertices[i]) - transform.InverseTransformPoint(new Vector3(0, _vertices[i].y, 0)));//*Random.Range( 0,2)
					//外顶点与内顶点移动时相对距离保持不变
                    //计算总顶点数除以每层单侧顶点数的商的奇偶关系来判断是外顶点还是内顶点
					//假设有16个顶点前8个是外圈后8个是内圈详情看GenerateCircle方法
					int n = i / SideCount;
					bool side = n % 2 == 0;//==true为外边
					//判断顶面顶点内外关系
					bool caps = (i - (SideCount * layer * 2)) % 2 == 0;
					//限制每个顶点最大和最小的移动距离
					float max;
					float min;
					if (i < SideCount * layer * 2)
					{
						//2:1.9
						max = side ? 2f * OuterRadius : 2f * OuterRadius - (OuterRadius - InnerRadius);

						min = side ? 0.5f * OuterRadius : 0.5f * OuterRadius - (OuterRadius - InnerRadius);
					}
					else
					{
						max = caps ? 2f * OuterRadius : 2f * OuterRadius - (OuterRadius - InnerRadius);
						min = caps ? 0.5f * OuterRadius : 0.5f * OuterRadius - (OuterRadius - InnerRadius);
					}
					//顶点到鼠标Y值之间的距离,余弦cos函数算出实际位移距离
					float dif = Mathf.Abs(info.point.y - transform.TransformPoint(_vertices[i]).y);
					//这里cos(0)=1固cos(0.5)>1 意思是距离越远dif越大 cos的值越小位移越小
					if (Input.GetKey(KeyCode.RightArrow)|| _playControl.useMouse(0) )
					{
						float outer = max- v_xz.magnitude;
						//outer *= Random.Range(1, 3f);//or //max*Random.Range(0,0.5f) And min*Random.Range(0,0.5f) //or//v_xz.magnitude*Random.Range(1,3f),min*=Random.Range(1,3f)//
						_vertices[i] += v_xz.normalized * Mathf.Min(0.01f * Mathf.Cos(((dif / 5 * Height) * Mathf.PI) / 2), outer);
					}
					else if (Input.GetKey(KeyCode.LeftArrow)||_playControl.useMouse(1))
					{
						float inner = v_xz.magnitude - min;
						//inner*=Random.Range(1,3f);
						_vertices[i] -= v_xz.normalized * Mathf.Min(0.01f * Mathf.Cos(((dif / 5 * Height) * Mathf.PI) / 2), inner);
					}
				}
				//Y轴
				float scale_y = transform.localScale.y;
				if (_playControl.useWhell()>0)
				{
					scale_y = Mathf.Min(transform.localScale.y + 0.000001f, 2.0f);
				}
				else if (_playControl.useWhell()<0)
				{

					scale_y = Mathf.Max(transform.localScale.y - 0.000001f, 0.3f);
				}
				transform.localScale = new Vector3(transform.localScale.x, scale_y, transform.localScale.z);

			}
			
			mesh.vertices = _vertices;
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			//NormalsReBuild();
			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;
		}
	}
	
	 [ContextMenu("ReBuildNormals")]
    void NormalsReBuild()
    {
        Mesh mesh = meshFilter.mesh;
        Vector3[] normals = mesh.normals;
        //上下法线平均
        for (int i = SideCount * 2; i < SideCount * 2 * (layer - 1); i++)
        {
            bool mark = i % 2 == 0;//偶数是 假设有3个圆柱这是第二个圆柱下面的点，
            if (mark)
            {
                normals[i] = (normals[i] + normals[(i + 1) - SideCount * 2]).normalized;
            }else if (!mark)  //bool mark = i % 2 == 0;//奇数是 假设有3个圆柱这是第二个圆柱上面的点，
            {
	            normals[i] = normals[i - 1 + SideCount * 2];
            }
        }

//        for (int i = SideCount * 2; i < SideCount * 2 * (layer - 1); i++)
//        {
//            bool mark = i % 2 == 0;//奇数是 假设有3个圆柱这是第二个圆柱上面的点，
//            if (!mark)
//            {
//                normals[i] = normals[i - 1 + SideCount * 2];
//            }
//        }
	    
        //第一层
        for (int i = 0; i < SideCount * 2; i++)
        {
            bool mark = i % 2 == 0;
            if (i < SideCount)
            {
                if (!mark)
                {
	                //第一层上面的法线normal[1]=normal[16]
	                //Debug.Log("规整"+normals[i] +"  __ "+normals[i - 1 + SideCount * 2]);
                    normals[i] = normals[i - 1 + SideCount * 2];
	                //Debug.Log(normals[i] +"  __ "+normals[i - 1 + SideCount * 2]);
                }
                else
                {
                    normals[i] = transform.TransformDirection(transform.InverseTransformPoint(mesh.vertices[i]) - transform.InverseTransformPoint(new Vector3(0, mesh.vertices[i].y, 0)));
                }
            }
            else//绘制内圈法线
            {
                if (!mark)
                {
                    normals[i] = normals[i - 1 + SideCount * 2];
                }
                else
                {	
	                normals[i] = transform.TransformDirection(transform.InverseTransformPoint(new Vector3(0, mesh.vertices[i].y, 0)) - transform.InverseTransformPoint(mesh.vertices[i]));
                }
            }

        }

        for (int i = 0; i < layer; i++)
        {
            //外侧 下面部分 首尾相接法线平均
            normals[SideCount * i * 2] = (normals[SideCount * i * 2] + normals[SideCount * (i * 2 + 1) - 2]).normalized;//0=(0+6)
            normals[SideCount * (i * 2 + 1) - 2] = normals[SideCount * i * 2];//6=0
	        //上面部分
            normals[SideCount * i * 2 + 1] = (normals[SideCount * i * 2 + 1] + normals[SideCount * (i * 2 + 1) - 1]).normalized;//1=(1+7)
            normals[SideCount * (i * 2 + 1) - 1] = normals[SideCount * i * 2 + 1];//7=1

            //内侧
            normals[SideCount * (i * 2 + 1)] = (normals[SideCount * (i * 2 + 1)] + normals[SideCount * (i + 1) * 2 - 2]).normalized;
            normals[SideCount * (i + 1) * 2 - 2] = normals[SideCount * (i * 2 + 1)];
	        
            normals[SideCount * (i * 2 + 1) + 1] = (normals[SideCount * (i * 2 + 1) + 1] + normals[SideCount * (i + 1) * 2 - 1]).normalized;
            normals[SideCount * (i + 1) * 2 - 1] = normals[SideCount * (i * 2 + 1) + 1];
        }
        //最上层
        for (int i = SideCount * 2 * (layer - 1); i < normals.Length; i++)
        {
            bool mark = i % 2 == 0;

            if (i < SideCount * (2 * layer - 1))
            {
                if (!mark)
                {
                    normals[i] = (normals[i] + normals[i + SideCount * 2 - 1]).normalized;
                }
            }
            else if (i < SideCount * layer * 2)
            {
                if (!mark)
                {
                    normals[i] = (normals[i] + normals[i + SideCount]).normalized;
                }
            }
            else
            {
                if (mark)
                {
                    normals[i] = normals[i - SideCount * 2 + 1];
                }
                else
                {
                    normals[i] = normals[i - SideCount];
                }
            }
        }
        mesh.normals = normals;
        meshFilter.mesh = mesh;
    }

	private Vector3 points;

	private void OnCollisionEnter(Collision other)
	{
		ContactPoint c = other.contacts[0];
		points = c.point;
		
	}
}
