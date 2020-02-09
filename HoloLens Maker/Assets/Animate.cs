using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class Animate : MonoBehaviour
{
    float rotation = 0;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        rotation += 36 * Time.deltaTime;
        this.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }
}
