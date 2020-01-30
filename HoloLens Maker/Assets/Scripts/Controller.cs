using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject arrowPrefab;
    public GameObject spherePrefab;
    public GameObject heatMapPrefab;

    public ArrayList arrowList = new ArrayList();
    public ArrayList sphereList = new ArrayList();
    public ArrayList heatList = new ArrayList();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CreateArrowPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newArrow = Instantiate(arrowPrefab, position, rotation);
        newArrow.name = "ArrowIndicator" + arrowList.Count;
        arrowList.Add(newArrow);
    }

    void CreateSpherePrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newSphere = Instantiate(spherePrefab, position, rotation);
        newSphere.name = "SphereIndicator" + sphereList.Count;
        sphereList.Add(newSphere);
    }

    void CreateHeatMapPrefab(Vector3 position, Quaternion rotation)
    {
        GameObject newHeatMap = Instantiate(heatMapPrefab, position, rotation);
        newHeatMap.name = "HeatMapIndicator" + heatList.Count;
        heatList.Add(newHeatMap);
    }
}
