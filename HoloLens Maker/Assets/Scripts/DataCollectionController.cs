using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataCollection;
using UnityEngine.UI;
using System.IO;

public class DataCollectionController : MonoBehaviour
{
    public static Parser MainCollection;
    public static FakeDataSource fakeSource;
    public bool isDebug;
    public GameObject Canvas;
    public GameObject Text;
    private int mode;
    // Start is called before the first frame update
    void Start()
    {
        fakeSource = new FakeDataSource("CLT Composite Beams Board1 2by10 Panel 1.ASC", 200);
        MainCollection = new Parser(fakeSource);
        MainCollection.start();
        if(isDebug == true)
        {
            Canvas = GameObject.Instantiate(Canvas);
            Canvas.transform.GetChild(1).GetComponent<Dropdown>().onValueChanged.AddListener(OnDropDownChange);
            mode = 0;

        }
        Debug.Log(Directory.GetCurrentDirectory());
    }


    public void OnDropDownChange(int value)
    {
        mode = value;
    }

    // Update is called once per frame
    void Update()
    {
        MainCollection.UpdateBeforeDraw();
        if (!isDebug)
            return;
        var elementGroup = Canvas.transform.GetChild(0);
        for( int i = elementGroup.childCount; i > 0; i--)
        {
            GameObject.Destroy(elementGroup.GetChild(i - 1).gameObject);
        }
        for (int i = 0; i < MainCollection.currentInfo.values.Length; i++)
        {
            DataPoint point = MainCollection.currentInfo.values[i];
            
            var newLabel = GameObject.Instantiate(Text, elementGroup);
            newLabel.GetComponent<Text>().text = point.sensorName;

            var newText = GameObject.Instantiate(Text, elementGroup);
            float displayValue = 0;
            switch(mode)
            {
                case 0:
                    displayValue = point.value;
                    break;
                case 1:
                    displayValue = point.deltaLastFrame;
                    break;
                case 2:
                    displayValue = point.deltaLastZero;
                    break;
            }
            newText.GetComponent<Text>().text = $"{displayValue:#0.0000} {point.unit}";
        }
    }

    private void OnDestroy()
    {
        MainCollection.Dispose();
        fakeSource.Dispose();
    }
}
