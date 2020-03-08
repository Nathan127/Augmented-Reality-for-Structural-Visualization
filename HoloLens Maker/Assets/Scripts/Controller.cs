//Main controller scripts that runs when the game starts.
//Used to Create and Update indicator sensors to be displayed on the HoloLens

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataCollection;
using UnityEngine.UI;
using System.IO;

public class Controller : MonoBehaviour
{
    //Set data collection sources, both fake and the real data from the Arduino
    public static Parser MainCollection;
    public static FakeDataSource fakeSource;
    public static SerialDataSource serialDataSource;

    //Allows prefabs to be easily set in the editor
    public GameObject arrowPrefab;
    public GameObject spherePrefab;
    public GameObject heatMapPrefab;

    //Creates lists to easily keep track of/modify sensors after they are created
    public List<GameObject> arrowList = new List<GameObject>();
    public List<GameObject> sphereList = new List<GameObject>();
    public List<GameObject> heatList = new List<GameObject>();

    //Used to make sure sensors are only instantiated once
    bool hasInstantiated = false;

    // Start is called before the first frame update
    void Start()
    {
        //Sets up both the fake data source and the real data from the ardiuno
        fakeSource = new FakeDataSource("CLT Composite Beams Board1 2by10 Panel 1.ASC", 200);
        serialDataSource = new SerialDataSource("Com3", new string[] {
        "DASYLab - V 11.00.00",
        "Worksheet name: 6by10beamlayout",
        "Recording date: 7 / 1 / 2016,  4:52:39 PM",
        "Block length: 2",
        "Delta: 1.0 sec.",
        "Number of channels: 3",
        "Date;Measurement time[hh:mm:ss];voltage [V];voltage2 [V]; volage3 [V];"});
        MainCollection = new Parser(serialDataSource);
        MainCollection.start();
    }

    // Update is called once per frame
    void Update()
    {
        MainCollection.UpdateBeforeDraw();
        //Make sure the data source has sensors ready and hasn't already been called before calling createSensors
        if(MainCollection.currentInfo.values.Length > 0)
        {
            if (hasInstantiated == false)
            {
                CreateSensors();
            }
        }

        //Keep the sensor's name, max, and min values up to date each frame
        for (int i = 0; i < MainCollection.currentInfo.values.Length; i++)
        {
            if(hasInstantiated)
            {
                sphereList[i].GetComponentInChildren<Indicator>().value = MainCollection.currentInfo.values[i].value;
                sphereList[i].GetComponentInChildren<Indicator>().valueMax = MainCollection.currentInfo.values[i].maxValue;
                sphereList[i].GetComponentInChildren<Indicator>().valueMin = MainCollection.currentInfo.values[i].minValue;
            }
        }
    }

    //Creates an instance of the arrow indicator and adds it to the arrow list to be tracked/modified
    void CreateArrowPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newArrow = Instantiate(arrowPrefab, position, rotation, transform);
        newArrow.name = "ArrowIndicator" + arrowList.Count;
        arrowList.Add(newArrow);
    }

    //Creates an instance of the sphere indicator and adds it to the arrow list to be tracked/modified
    void CreateSpherePrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newSphere = Instantiate(spherePrefab, position, rotation, transform);
        newSphere.name = "SphereIndicator" + sphereList.Count;
        sphereList.Add(newSphere);
    }

    //Creates an instance of the heatmap indicator and adds it to the arrow list to be tracked/modified
    void CreateHeatMapPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newHeatMap = Instantiate(heatMapPrefab, position, rotation,transform);
        newHeatMap.name = "HeatMapIndicator" + heatList.Count;
        heatList.Add(newHeatMap);
    }

    //Cleanup after program is ended
    private void OnDestroy()
    {
        MainCollection.Dispose();
        fakeSource.Dispose();
        serialDataSource.Dispose();
    }

    //Instantiates sensors after the data source is set up. Sets a bool to ensure sensors are only created once
    private void CreateSensors()
    {
        hasInstantiated = true;

        for (int i = 0; i < MainCollection.currentInfo.values.Length; i++)
        {
            CreateSpherePrefab(MainCollection.currentInfo.values[i].position, Quaternion.identity);
            sphereList[i].GetComponentInChildren<Indicator>().colorMode = true;
            sphereList[i].GetComponentInChildren<Indicator>().sensorName = MainCollection.currentInfo.values[i].sensorName;
        }
    }
}
