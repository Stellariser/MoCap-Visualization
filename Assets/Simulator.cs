using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;

public class Simulator : MonoBehaviour
{
    public GameObject cathTop, cathTL, cathTR, cathBL, cathBR,
                    skullTL, skullTR, skullBL, skullBR, skullBrow; //references to marker spheres
    public GameObject point1Sphere, point2Sphere, catheder, skull, parent;
    float timeToCall;
    float timeDelay = 1.0f; //the code will be run every 2 seconds
    const string separator = "\t"; //tab separation string
    string path = "Assets/catheter008.txt"; //path to tsv file
    int index, fileSize, loopIndex=0, stopIndex=2; //index to cycle through arrays
    bool readyToUpdate;

    //arrays with data from each row
    float[] field, time;
    float[,] headTopLeft, headTopRight, headBottomLeft, headBottomRight, headHole, headBrow,
            cathTip, cathTopLeft, cathTopRight, cathBottomLeft, cathBottomRight, cathEnd;

    //coordinates for 3D objects transform update
    private float x1, x2, x3, x4, x5, x6, x7, x8, x9, x10,
        y1, y2, y3, y4, y5, y6, y7, y8, y9, y10,
        z1, z2, z3, z4, z5, z6, z7, z8, z9, z10;

    public Slider slider; //slider to control the animation speed

    // Start is called before the first frame update
    void Start()
    {
        //initialize indexes
        index = fileSize = 0;
        readyToUpdate = false;

        slider.onValueChanged.AddListener(delegate { ChangeSpeed(); });

        timeToCall = Time.fixedTime + timeDelay;

        StreamReader sr = ReadFile(path); //read from file
        fileSize = FindSize(sr); //find size of file

        //initialize arrays
        field = time = new float[fileSize];
        headTopLeft = new float[fileSize, 3];
        headTopRight = new float[fileSize, 3];
        headBottomLeft = new float[fileSize, 3];
        headBottomRight = new float[fileSize, 3];
        headHole = new float[fileSize, 3];
        headBrow = new float[fileSize, 3];
        cathTip = new float[fileSize, 3];
        cathTopLeft = new float[fileSize, 3];
        cathTopRight = new float[fileSize, 3];
        cathBottomLeft = new float[fileSize, 3];
        cathBottomRight = new float[fileSize, 3];
        cathEnd = new float[fileSize, 3];

        //extract and distribute info
        sr.DiscardBufferedData();
        sr.BaseStream.Seek(0, SeekOrigin.Begin);
        Extract(sr);
        readyToUpdate = true;

        //close reader
        sr.Close();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.fixedTime >= timeToCall && MarkerCheck() && fileSize > 0 && readyToUpdate&&loopIndex<stopIndex)
        {
            //normalize positions
            if (index >= fileSize)
            {
                index = 0; //stop simulation if eod is reached
                loopIndex++;
                //TODO Display UI
            }
            Normalize();
            index++;
           
            //update marker positions
            cathTop.transform.localPosition = new Vector3(x1, y1, z1);
            cathTL.transform.localPosition = new Vector3(x2, y2, z2);
            cathTR.transform.localPosition = new Vector3(x3, y3, z3);
            cathBL.transform.localPosition = new Vector3(x4, y4, z4);
            cathBR.transform.localPosition = new Vector3(x5, y5, z5);
            skullTL.transform.localPosition = new Vector3(x6, y6, z6);
            skullTR.transform.localPosition = new Vector3(x7, y7, z7);
            skullBL.transform.localPosition = new Vector3(x8, y8, z8);
            skullBR.transform.localPosition = new Vector3(x9, y9, z9);
            skullBrow.transform.localPosition = new Vector3(x10, y10, z10);

         

            timeToCall = Time.fixedTime + timeDelay;


            //map catheder
            Vector3 Top = new Vector3(x1, y1, z1);
            Vector3 TL = new Vector3(x2, y2, z2);
            Vector3 TR = new Vector3(x3, y3, z3);
            Vector3 BL = new Vector3(x4, y4, z4);
            Vector3 BR = new Vector3(x5, y5, z5);
            Vector3 point1 = Vector3.Project(Top - TL, TR - TL) + TL;
            point1Sphere.transform.localPosition = point1;
            Vector3 point2 = Vector3.Project(Top - BL, BR - BL) + BL;
            point2Sphere.transform.localPosition = point2;
            catheder.transform.localPosition = point1;
            catheder.transform.LookAt(parent.transform.TransformPoint(point2));

            //map skull
            Vector3 brow = new Vector3(x10, y10, z10);
            Vector3 skullL = new Vector3(x6, y6, z6);
            Vector3 skullR = new Vector3(x7, y7, z7);
            skull.transform.localPosition = skullL;
            skull.transform.LookAt(parent.transform.TransformPoint(skullR));
            skull.transform.localPosition = brow;


        }
        else Debug.Log("nope");

    }

    //method to normalize coordinates in Unity scene
    private void Normalize()
    {
        //x coordinate
        x1 = cathTip[index, 0] / 1000.0f;
        x2 = cathTopLeft[index, 0] / 1000.0f;
        x3 = cathTopRight[index, 0] / 1000.0f;
        x4 = cathBottomLeft[index, 0] / 1000.0f;
        x5 = cathBottomRight[index, 0] / 1000.0f;
        x6 = headTopLeft[index, 0] / 1000.0f;
        x7 = headTopRight[index, 0] / 1000.0f;
        x8 = headBottomLeft[index, 0] / 1000.0f;
        x9 = headBottomRight[index, 0] / 1000.0f;
        x10 = headBrow[index, 0] / 1000.0f;
        
        //y coordinate
        y1 = cathTip[index, 1] / 1000.0f;
        y2 = cathTopLeft[index, 1] / 1000.0f;
        y3 = cathTopRight[index, 1] / 1000.0f;
        y4 = cathBottomLeft[index, 1] / 1000.0f;
        y5 = cathBottomRight[index, 1] / 1000.0f;
        y6 = headTopLeft[index, 1] / 1000.0f;
        y7 = headTopRight[index, 1] / 1000.0f;
        y8 = headBottomLeft[index, 1] / 1000.0f;
        y9 = headBottomRight[index, 1] / 1000.0f;
        y10 = headBrow[index, 1] / 1000.0f;
        
        //z coordinate
        z1 = cathTip[index, 2] / 1000.0f;
        z2 = cathTopLeft[index, 2] / 1000.0f;
        z3 = cathTopRight[index, 2] / 1000.0f;
        z4 = cathBottomLeft[index, 2] / 1000.0f;
        z5 = cathBottomRight[index, 2] / 1000.0f;
        z6 = headTopLeft[index, 2] / 1000.0f;
        z7 = headTopRight[index, 2] / 1000.0f;
        z8 = headBottomLeft[index, 2] / 1000.0f;
        z9 = headBottomRight[index, 2] / 1000.0f;
        z10 = headBrow[index, 2] / 1000.0f;
    }

    //function to check if objects assigned to markers are not null
    private bool MarkerCheck()
    {
        if (cathTop != null && cathTL != null && cathTR != null && cathBL != null && cathBR != null
            && skullBL != null && skullBR != null && skullTL != null && skullTR != null && skullBrow != null)
            return true;
        else return false;
    }

    //function to read the file with recorded MoCap data
    private StreamReader ReadFile(string path)
    {
        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine(); //first line = headers
        return reader;
    }

    //function to find the total number of lines in the file being read
    private int FindSize(StreamReader reader)
    {
        int i = 1;
        string line = reader.ReadLine();
        while (line != null)
        {
            i++;
            line = reader.ReadLine();
        }
        return i;
    }

    //method to extract coordinates from the file being read
    private void Extract(StreamReader reader)
    {
        string line;
        for (int i = 0; i < 5; i++) 
            line = reader.ReadLine(); //skip headers
        line = reader.ReadLine(); //first line

        //extract info and distribute
        while (line != null && line != "") //interrupt at empty line or end of file
        {
            string[] temp = line.Split(separator.ToCharArray());
            int runtimeField = Int32.Parse(temp[0]); //current array id

            //populate arrays
            field[runtimeField] = runtimeField + 1.0f;
            time[runtimeField] = runtimeField/100.0f;
            
            //marker tree attached to the skull
            headTopLeft[runtimeField, 0] = float.Parse(temp[2]); //skull 1 x
            headTopLeft[runtimeField, 1] = float.Parse(temp[4]); //skull 1 y
            headTopLeft[runtimeField, 2] = float.Parse(temp[3]); //skull 1 z
            headTopRight[runtimeField, 0] = float.Parse(temp[5]); //skull 2 x
            headTopRight[runtimeField, 1] = float.Parse(temp[7]); //skull 2 y
            headTopRight[runtimeField, 2] = float.Parse(temp[6]); //skull 2 z
            headBottomLeft[runtimeField, 0] = float.Parse(temp[8]); //skull 3 x
            headBottomLeft[runtimeField, 1] = float.Parse(temp[10]); //skull 3 y
            headBottomLeft[runtimeField, 2] = float.Parse(temp[9]); //skull 3 z
            headBottomRight[runtimeField, 0] = float.Parse(temp[11]); //skull 4 x
            headBottomRight[runtimeField, 1] = float.Parse(temp[13]); //skull 4 y
            headBottomRight[runtimeField, 2] = float.Parse(temp[12]); //skull 4 z
            
            /* calibration marker on burr hole is always 0
            headHole[runtimeField, 0] = float.Parse(temp[14]); //burr hole x
            headHole[runtimeField, 1] = float.Parse(temp[16]); //burr hole y
            headHole[runtimeField, 2] = float.Parse(temp[15]); //burr hole z
            */

            //marker attached to the skull brow
            headBrow[runtimeField, 0] = float.Parse(temp[17]); //skull brow x
            headBrow[runtimeField, 1] = float.Parse(temp[19]); //skull brow y
            headBrow[runtimeField, 2] = float.Parse(temp[18]); //skull brow z
            
            //marker tree attached to the catheter
            cathTip[runtimeField, 0] = float.Parse(temp[20]); //catheter 1 x
            cathTip[runtimeField, 1] = float.Parse(temp[22]); //catheter 1 y
            cathTip[runtimeField, 2] = float.Parse(temp[21]); //catheter 1 z
            cathTopLeft[runtimeField, 0] = float.Parse(temp[23]); //catheter 2 x
            cathTopLeft[runtimeField, 1] = float.Parse(temp[25]); //catheter 2 y
            cathTopLeft[runtimeField, 2] = float.Parse(temp[24]); //catheter 2 z
            cathTopRight[runtimeField, 0] = float.Parse(temp[26]); //catheter 3 x
            cathTopRight[runtimeField, 1] = float.Parse(temp[28]); //catheter 3 y
            cathTopRight[runtimeField, 2] = float.Parse(temp[27]); //catheter 3 z
            cathBottomLeft[runtimeField, 0] = float.Parse(temp[29]); //catheter 4 x
            cathBottomLeft[runtimeField, 1] = float.Parse(temp[31]); //catheter 4 y
            cathBottomLeft[runtimeField, 2] = float.Parse(temp[30]); //catheter 4 z
            cathBottomRight[runtimeField, 0] = float.Parse(temp[32]); //catheter 5 x
            cathBottomRight[runtimeField, 1] = float.Parse(temp[34]); //catheter 5 y
            cathBottomRight[runtimeField, 2] = float.Parse(temp[33]); //catheter 5 z
            
            /* calibration marker on catheter tip is always 0
            cathEnd[runtimeField, 0] = float.Parse(temp[32]); //catheter tip x
            cathEnd[runtimeField, 1] = float.Parse(temp[34]); //catheter tip y
            cathEnd[runtimeField, 2] = float.Parse(temp[33]); //catheter tip z
            */

            line = reader.ReadLine();
        }
    }

    private void ChangeSpeed()
    {
        timeDelay = slider.value;
    }
}
