using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLink : MonoBehaviour
{

    List<GameObject> m_objArray = new List<GameObject> ();

    List<Vector3> data = new List<Vector3>();

    DrawCurve draw_curve_ ;

    float width = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        Fill();

        if (draw_curve_ == null)
            draw_curve_ = gameObject.AddComponent<DrawCurve>();
        draw_curve_.SetLineColor(new Color(0.0f, 1.0f, 0.0f));
        draw_curve_.SetLineWidth(width);
    }

    // Update is called once per frame
    void Update()
    {
        draw_curve_.SetLineWidth(width);
        data.Clear();

        for(int i=0;i<m_objArray.Count;i++)
        {
            data.Add(m_objArray[i].transform.position);
        }
        data.Add(m_objArray[0].transform.position);
        draw_curve_.OnDraw(ref data);
        
    }

    void Fill()
    {
        foreach (Transform child in transform)
        {
            m_objArray.Add(child.gameObject);
        }
    }
}
