using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sensor : MonoBehaviour
{

    // Use this for initialization

    public float sensor_ConeAngle = 30f*(float)Math.PI/180f;

    public MeshFilter sensor_mf_;
    public MeshRenderer sensor_mr_;
    public Mesh sensor_mesh;
    Material sensor_material_;
    string attachedName = "";

    Color color = new Color(1.0f,0.0f,0.0f);
    List<Vector3> data; //覆盖范围轮廓
    

    GameObject sat;

    GameObject earth;
    void Start()
    {
        Init();

        Vector3 v = gameObject.transform.position;
        //gameObject.transform.localRotation = Quaternion.Euler(180f, 0, 0); ;
        SetColor(color.r, color.g, color.b, 0.5f);
        InitSphericalSensor(300, sensor_ConeAngle);
    }
    // Update is called once per frame
    void Update()
    {
   
    }

    void Init()
    {
        sensor_mf_ = gameObject.GetComponent<MeshFilter>();
        sensor_mr_ = gameObject.GetComponent<MeshRenderer>();

        if (sensor_mf_ == null)
            sensor_mf_ = gameObject.AddComponent<MeshFilter>();

        if (sensor_mr_ == null)
            sensor_mr_ = gameObject.AddComponent<MeshRenderer>();

        if (sensor_mesh == null)
        {
            sensor_mesh = new Mesh();

            sensor_mf_.mesh = sensor_mesh;
        }

        if (sensor_material_ == null)
        {
            sensor_material_ = new Material(Shader.Find("Transparent/Diffuse"));
            sensor_mr_.material = sensor_material_;
        }
    }
    private void FixedUpdate()
    {
        //实时获取绘制
        
    }

    ///设置颜色与透明度
    public void SetColor(float r,float g,float b,float a)
    {
        if (sensor_material_ == null)
            Init();

        sensor_material_.color = new Color(r, g, b, a);
    }
    void InitMesh()
    {
        sensor_mesh.name = "MyMesh";

        // 为网格创建顶点数组
        const int i = 5;
        Vector3[] vertices = new Vector3[i]{
            new Vector3(0, 0, 0),
            new Vector3(3, 0, 4),
            new Vector3(0, -3, 4),
            new Vector3(-3, 0, 4),
            new Vector3(0,3,4)
        };
        sensor_mesh.vertices = vertices;

        // 通过顶点为网格创建三角形,顶点数组的小标
        int[] triangles = new int[3 * 4]{
            0, 1, 2, 0, 2, 3,0,3,4,0,4,1
        };

        sensor_mesh.triangles = triangles;
    }
    void InitSphericalSensor(float radius,float angle)
    {
        sensor_mesh.name = "MyMesh";

        // 为网格创建顶点数组
        const int vNum =50;
        Vector2[] vertices = new Vector2[vNum];

        float l = radius * Mathf.Tan(angle);
        for(int k=0;k<vNum;k++)
        {
            Vector2 v = new Vector2(l*Mathf.Cos(2.0f * Mathf.PI * k / vNum),
                    l*Mathf.Sin(2.0f * Mathf.PI * k / vNum));
            vertices[k] = v;
        }

        DrawCone.SetMesh(gameObject, radius, vertices); 
    }

    public void SetSensorAngle(float angle)
    {
        sensor_ConeAngle = angle;
    }

    public void DrawSensor()
    {
        //获取开始时刻该卫星下传感器的覆盖经纬度范围
    }

    public void CreateObject()
    {
        Init();
        
    }

    private void InitComplexSensor(float length, float v1, float v2, float v3, float v4)
    {
        sensor_mesh.name = "MyMesh";

        // 为网格创建顶点数组
        const int vNum = 21;
        Vector2[] vertices = new Vector2[vNum*2];

        float l1 = length * Mathf.Tan(v1);
        float l2 = length * Mathf.Tan(v2);

        float deltaAngle = (v4 - v3) / (vNum-1);
        for (int k = 0; k < vNum; k++)
        {
            Vector2 v = new Vector2(l2 * Mathf.Cos(v3+k*deltaAngle),
                    l2 * Mathf.Sin(v3 + k * deltaAngle));
            vertices[k] = v;
        }
        for (int k = 0; k < vNum; k++)
        {
            Vector2 v = new Vector2(l1 * Mathf.Cos(v4 - k * deltaAngle),
                    l1 * Mathf.Sin(v4 - k * deltaAngle));
            vertices[k+vNum] = v;
        }
        DrawCone.SetMesh(gameObject, length, vertices);
    }

    private void InitRectangleSensor(float length, float v1, float v2)
    {
        float l1 = length * Mathf.Tan(v1);
        float l2 = length * Mathf.Tan(v2);


        sensor_mesh.name = "MyMesh";

        // 为网格创建顶点数组
        Vector2[] vertices = new Vector2[4];
        vertices[0] = new Vector2(l1, l2);
        vertices[1] = new Vector2(-l1, l2);
        vertices[2] = new Vector2(-l1, -l2);
        vertices[3] = new Vector2(l1, -l2);

      

        DrawCone.SetMesh(gameObject, length, vertices);

    }
   
}