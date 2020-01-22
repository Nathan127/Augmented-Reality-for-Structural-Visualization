using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    public string sensorName;
    public float value;
    public Vector3 position;

    public float valueMax;
    public float valueMin;

    [Range(1.0f, 10.0f)]
    public float size = 1;
    //ToDo, fade between two colors
    public float color;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = position;
        transform.localScale += new Vector3(1, 1, 1) * size;
    }
}
