//This script is instanced to be unique for every sensor created by the controller script
//Used to set the size, color, or text label of individual sensors

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    //These values will be set by the data source
    public string sensorName;
    public float value;
    public Vector3 position;
    public float valueMax;
    public float valueMin;
    
    //Percentage is calculated based on the min/max and current value
    private float percentage;

    //Used to lerp between two colors
    public Color minColor = Color.blue;
    public Color maxColor = Color.red;
    private Color lerpedColor;

    //These are both set to off by default and are changed in the editor as needed
    public bool colorMode = false;
    public bool sizeMode = false;

    //Used to add the name and value over the sensor indicator
    public GameObject TextMesh;
  
    void Update()
    {
        //Set the sensor name and keep it updated with the current value
        TextMesh.GetComponent<TextMeshPro>().text = $"{sensorName}: {value}";
        TextMesh.GetComponent<TextMeshPro>().transform.rotation = Quaternion.LookRotation(this.transform.position - Camera.main.transform.position );
        
        //Update color with lerp, otherwise default to minColor
        if (colorMode)
        {
            SetColor();
        }
        else
        {
            GetComponent<Renderer>().material.SetColor("_Color", minColor);
        }

        //Update size with percentage, otherwise default to one
        if(sizeMode)
        {
            SetSize();
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    //Called in update to keep updated
    float calculatePercentage(float input)
    {
        return ((input - valueMin)) / (valueMax - valueMin);
    }

    //Called in update to keep updated
    void SetColor()
    {
        percentage = calculatePercentage(value);
        //Lerp is a built in function that interoplates between 2 values, given a percentage
        lerpedColor = Color.Lerp(minColor, maxColor, percentage);
        GetComponent<Renderer>().material.SetColor("_Color", lerpedColor);
    }

    //Called in update to keep updated
    void SetSize()
    {
        percentage = calculatePercentage(value);
        transform.localScale = new Vector3(1, 1, 1) * percentage;
    }
}
