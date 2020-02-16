using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    public string sensorName;
    public float value;
    public Vector3 position;

    public float valueMax;
    public float valueMin;
    private float percentage;

    private Color lerpedColor;
    public Color minColor = Color.blue;
    public Color maxColor = Color.red;

    public bool colorMode = false;
    public bool sizeMode = false;

    public GameObject TextMesh;

    float calculatePercentage(float input)
    {
        return ((input - valueMin)) / (valueMax - valueMin);
    }

    void SetColor()
    {
        percentage = calculatePercentage(value);
        lerpedColor = Color.Lerp(minColor, maxColor, percentage);
        GetComponent<Renderer>().material.SetColor("_Color", lerpedColor);
    }

    void SetSize()
    {
        percentage = calculatePercentage(value);
        transform.localScale = new Vector3(1, 1, 1) * percentage;
    }

    void Update()
    {
        TextMesh.GetComponent<TextMeshPro>().text = $"{sensorName}: {value}";
        TextMesh.GetComponent<TextMeshPro>().transform.rotation = Quaternion.LookRotation(this.transform.position - Camera.main.transform.position );


        if (colorMode)
        {
            SetColor();
        }
        else
        {
            GetComponent<Renderer>().material.SetColor("_Color", minColor);
        }

        if(sizeMode)
        {
            SetSize();
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }
}
