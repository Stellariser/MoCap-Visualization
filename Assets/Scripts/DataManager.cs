using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;

public class DataManager : MonoBehaviour
{
    public Transform catheterTip, catheterTop, catheterTipAnimated, catheterTopAnimated;
    public GameObject planeObj;
    bool isAnimation;
    Plane plane;
    bool movementDirDown = true;
    float prevPos; //prevPos of catheter tip to calculate movement direction
    List<dataEntry> data;
    List<dataEntry> condition1 = new List<dataEntry>(); //smoothed
    List<dataEntry> condition2 = new List<dataEntry>(); //real data
    List<dataEntry> condition3 = new List<dataEntry>(); //animation
    public LayerMask planeMask;
    [HideInInspector]
    public string startTime;
    bool started;
    struct dataEntry
    {
        public string condition;
        public float dist;
        public string movDir;
        public float distCatheter;
        public string timestamp;

        public dataEntry(float dist, float angledDist, bool movDir, string timestamp, string condition = "")
        {
            this.dist = dist;
            this.distCatheter = angledDist;
            this.movDir = movDir ? "down" : "up";
            this.timestamp = timestamp;
            this.condition = condition;
        }
        public string getAsString()
        {
            if (condition == "")
                return dist + ";" +distCatheter+";" + movDir + ";" + timestamp + "; ";
            else return " ;;" + timestamp + ";" + condition; //write condition or end condition to file
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
    public void recalibrate()
    {
        //recalculates plane vectors for distance calculation
        var filter = planeObj.GetComponent<MeshFilter>();
        Vector3 normal;

        if (filter && filter.mesh.normals.Length > 0)
        {
            normal = filter.transform.TransformDirection(filter.mesh.normals[0]);
            plane = new Plane(normal, planeObj.transform.position);
        }
    }

    public void startDataRecording(bool isAnimation, bool isAveraged)
    {
        started = true;
        int condition = getConditionNumber(isAnimation, isAveraged);
        this.isAnimation = isAnimation;
        data = isAnimation ? condition3 : isAveraged ? condition1 : condition2;
        data.Add(new dataEntry(0, 0,false, getTime(), "Category " + condition));
        //InvokeRepeating("calcTruth", 1, 0.2f); //uncomment if you want to calculate ground truth
    }
    public void endDataRecording()
    {
        data.Add(new dataEntry(0, 0,false, getTime(), "endOfCondition"));
    }
    int getConditionNumber(bool isAnimation, bool isAveraged)
    {
        return isAnimation ? 3 : isAveraged ? 1 : 2;
    }
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if(started)
                data.Add(new dataEntry(plane.GetDistanceToPoint(isAnimation ? catheterTipAnimated.position : catheterTip.position), calcAngledDist(), movementDirDown, getTime()));
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
            Invoke("stopVibration", 0.2f);
        }
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
            Invoke("stopVibration", 0.4f);
            WriteData(); //write data to file

            //reset data for new trial
            data = new List<dataEntry>(); //
            condition1 = new List<dataEntry>(); //smoothed
            condition2 = new List<dataEntry>(); //real data
            condition3 = new List<dataEntry>(); //animation
            started = false;
        }

    }
    void stopVibration()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }
    string getTime()
    {
        return System.DateTime.UtcNow.ToString();
    }
    void calcDir()
    {
        if (!isAnimation)
        {
            movementDirDown = catheterTip.position.y - prevPos < 0;
            prevPos = catheterTip.position.y;
        }
        else
        {
            movementDirDown = catheterTipAnimated.position.y - prevPos < 0;
            prevPos = catheterTipAnimated.position.y;
        }
    }

    float calcAngledDist()
    {
        RaycastHit hit;
        if (!isAnimation && Physics.Raycast(catheterTop.position, catheterTip.position - catheterTop.position, out hit, planeMask))
        {
            if (catheterTip.position.y > planeObj.transform.position.y)        
                return Vector3.Distance(hit.point, catheterTip.position);
            else           
                return -Vector3.Distance(hit.point, catheterTip.position);
        }

        else if (Physics.Raycast(catheterTopAnimated.position, catheterTipAnimated.position - catheterTopAnimated.position, out hit, planeMask))
        {
            if (catheterTipAnimated.position.y > planeObj.transform.position.y)
                return Vector3.Distance(hit.point, catheterTipAnimated.position);
            else            
                return -Vector3.Distance(hit.point, catheterTipAnimated.position);
        }
        return 0;
    }
    //ground truth calculation
    public float groundTruthDist;
    public float groundTruthAngledDist;
    void calcTruth()
    {
        float currDist = plane.GetDistanceToPoint(isAnimation ? catheterTipAnimated.position : catheterTip.position);
        float currAngledDist = calcAngledDist();
        if (currDist < groundTruthDist) groundTruthDist = currDist;
        if (currAngledDist < groundTruthAngledDist) groundTruthAngledDist = currAngledDist;
    }
    void WriteData()
    {
        StringBuilder sb = new StringBuilder();
        for (int index = 0; index < condition1.Count; index++)
            sb.AppendLine(condition1[index].getAsString());
        for (int index = 0; index < condition2.Count; index++)
            sb.AppendLine(condition2[index].getAsString());
        for (int index = 0; index < condition3.Count; index++)
            sb.AppendLine(condition3[index].getAsString());
        string filePath = Application.persistentDataPath + "/results" + startTime + ".csv";
        StreamWriter outStream = File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
        Debug.Log("data saved at: "+Application.persistentDataPath);
    }
}