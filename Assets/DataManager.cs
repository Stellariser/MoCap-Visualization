using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;

public class DataManager : MonoBehaviour
{
    public Transform catheterTip, catheder;
    public GameObject planeObj;
    bool movementDirDown = true;
    Plane plane;
    float prevPos;
    List<dataEntry> data = new List<dataEntry>();
    struct dataEntry
    {       
        public float dist;
        public bool movDir;
        public string timestamp;

        public dataEntry(float dist, bool movDir, string timestamp)
        {
            this.dist = dist;
            this.movDir = movDir;
            this.timestamp = timestamp;
        }
        public string getAsString()
        {
            return dist + ";" + movDir + ";" + timestamp;
        }
    }

    void Start()
    {
        var filter = planeObj.GetComponent<MeshFilter>();
        Vector3 normal;

        if (filter && filter.mesh.normals.Length > 0)
        {
            normal = filter.transform.TransformDirection(filter.mesh.normals[0]);
            plane = new Plane(normal, planeObj.transform.position);
        }
        InvokeRepeating("calcDir", 1, 1);
    }

    // Update is called once per frame
    void Update()
    {

        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown("a"))
        {
            Debug.Log("button pressed");
            Debug.Log(plane.GetDistanceToPoint(catheterTip.position));
            data.Add(new dataEntry(plane.GetDistanceToPoint(catheterTip.position), movementDirDown, getTime()));
        }

    }
    string getTime()
    {
        return System.DateTime.UtcNow.ToString(); //TODO calculate based on start time??;
    }
    //TODO needs more stable Implementation once we decided on the data
    void calcDir()
    {
        movementDirDown = catheterTip.position.y - prevPos < 0;
        prevPos = catheterTip.position.y;
        Debug.Log(movementDirDown);
    }
    private void OnDestroy()
    {
        WriteData();
    }

    void WriteData()
    {

        StringBuilder sb = new StringBuilder();
        for (int index = 0; index < data.Count; index++)
            sb.AppendLine(data[index].getAsString());
        print(Application.dataPath);
        string filePath = Application.dataPath + "/results" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";

        StreamWriter outStream = File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
    }
}