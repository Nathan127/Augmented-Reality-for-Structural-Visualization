using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataCollection;
using UnityEngine.UI;
using System.IO;

public class Controller : MonoBehaviour
{
    public static Parser MainCollection;
    public static FakeDataSource fakeSource;
    public static SerialDataSource serialDataSource;

    public GameObject arrowPrefab;
    public GameObject spherePrefab;
    public GameObject heatMapPrefab;

    public List<GameObject> arrowList = new List<GameObject>();
    public List<GameObject> sphereList = new List<GameObject>();
    public List<GameObject> heatList = new List<GameObject>();

    bool hasInstantiated = false;

    // Start is called before the first frame update
    void Start()
    {
        fakeSource = new FakeDataSource("CLT Composite Beams Board1 2by10 Panel 1.ASC", 200);
        serialDataSource = new SerialDataSource("Com3", new string[] {
        "DASYLab - V 11.00.00",
        "Worksheet name: 6by10beamlayout",
        "Recording date: 7 / 1 / 2016,  4:52:39 PM",
        "Block length: 2",
        "Delta: 1.0 sec.",
        "Number of channels: 2",
        "Date;Measurement time[hh:mm:ss];voltage [V];voltage2 [V];"});
        MainCollection = new Parser(serialDataSource);
        MainCollection.start();
    }

    // Update is called once per frame
    void Update()
    {
        MainCollection.UpdateBeforeDraw();
        if(MainCollection.currentInfo.values.Length > 0)
        {
            if (hasInstantiated == false)
            {
                CreateSensors();
            }
        }

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

    void CreateArrowPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newArrow = Instantiate(arrowPrefab, position, rotation, transform);
        newArrow.name = "ArrowIndicator" + arrowList.Count;
        arrowList.Add(newArrow);
    }

    void CreateSpherePrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newSphere = Instantiate(spherePrefab, position, rotation, transform);
        newSphere.name = "SphereIndicator" + sphereList.Count;
        sphereList.Add(newSphere);
    }

    void CreateHeatMapPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newHeatMap = Instantiate(heatMapPrefab, position, rotation,transform);
        newHeatMap.name = "HeatMapIndicator" + heatList.Count;
        heatList.Add(newHeatMap);
    }

    private void OnDestroy()
    {
        MainCollection.Dispose();
        fakeSource.Dispose();
        serialDataSource.Dispose();
    }

    private void CreateSensors()
    {
        hasInstantiated = true;

        for (int i = 0; i < MainCollection.currentInfo.values.Length; i++)
        {
            CreateSpherePrefab(MainCollection.currentInfo.values[i].position, Quaternion.identity);
            sphereList[i].GetComponentInChildren<Indicator>().colorMode = true;
        }
    }
}
