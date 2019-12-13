/*********************************
Copyright:cug
Author:zhangkai
Date:2018-11-26
Description:draw orbit curves
*********************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class DrawCurve : MonoBehaviour
{

    // Use this for initialization
    private LineRenderer line_renderer_;
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LineRenderInit()
    {
        line_renderer_ = gameObject.GetComponent<LineRenderer>();
        if (line_renderer_ == null)
            line_renderer_ = gameObject.AddComponent<LineRenderer>();

        line_renderer_.material = new Material(Shader.Find("Particles/Additive"));
    }
    public void SetLineWidth(float width)
    {

        if (line_renderer_ == null)
            LineRenderInit();

        line_renderer_.startWidth = width;
        line_renderer_.endWidth = width;
    }

    public void SetLineColor(Color color)
    {

        if (line_renderer_ == null)
            LineRenderInit();

        line_renderer_.material = new Material(Shader.Find("Sprites/Default"));
        line_renderer_.startColor = color;
        line_renderer_.endColor = color;
    }

    void DefaultSetting()
    {
    }
    //画线,并设置是否在世界坐标系下进行画线,如若为false,则在局部坐标系下进行绘制
    public void OnDraw(ref List<Vector3> position_data, bool isWorld = true)
    {
        // gameObject.
        if (line_renderer_ == null)
            LineRenderInit();
       // DefaultSetting();
        line_renderer_.useWorldSpace = isWorld;
        line_renderer_.positionCount = position_data.Count;
        for (int i = 0; i < position_data.Count; i++)
        {
            line_renderer_.SetPosition(i, position_data[i]);
        }
    }

}
