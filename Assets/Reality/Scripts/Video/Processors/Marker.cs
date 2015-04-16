using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Threading;
using Assets.Scripts.Processors;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Processors
{
    public class Marker
    {
        public MCvScalar markerMinScalar;
        public MCvScalar markerMaxScalar;

        public Image<Gray, byte> thresh;
        public Image<Gray, byte> bounds;
        public List<WeightedPoint> points;

        float[] markerMin;
        float[] markerMax;

        string name;

        public void saveToPlayerPrefs()
        {
            Debug.Log("Saving marker " + name);
            PlayerPrefs.SetFloat(name + "_h_max", markerMax[0]);
            PlayerPrefs.SetFloat(name + "_s_max", markerMax[1]);
            PlayerPrefs.SetFloat(name + "_v_max", markerMax[2]);
            PlayerPrefs.SetFloat(name + "_h_min", markerMin[0]);
            PlayerPrefs.SetFloat(name + "_s_min", markerMin[1]);
            PlayerPrefs.SetFloat(name + "_v_min", markerMin[2]);
        }

        public void loadFromPlayerPrefs()
        {
            Debug.Log("Loading marker " + name);
            markerMax[0] = PlayerPrefs.GetFloat(name + "_h_max");
            markerMax[1] = PlayerPrefs.GetFloat(name + "_s_max");
            markerMax[2] = PlayerPrefs.GetFloat(name + "_v_max");
            markerMin[0] = PlayerPrefs.GetFloat(name + "_h_min");
            markerMin[1] = PlayerPrefs.GetFloat(name + "_s_min");
            markerMin[2] = PlayerPrefs.GetFloat(name + "_v_min");

            updateMaxMarker();
            updateMinMarker();
        }

        public Marker(String name)
        {
            this.name = name;

            markerMin = new float[3] { 0, 0, 0 };
            markerMax = new float[3] { 255, 255, 255 };
            updateMaxMarker();
            updateMinMarker();

            thresh = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
            bounds = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);

            points = new List<WeightedPoint>();

            loadFromPlayerPrefs();

        }

        public Marker(String name, float hMin, float sMin, float vMin, float hMax, float sMax, float vMax)
        {
            this.name = name;

            markerMin = new float[3] { hMin, sMin, vMin };
            markerMax = new float[3] { hMax, sMax, vMax };
            updateMaxMarker();
            updateMinMarker();

            thresh = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
            bounds = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);

            points = new List<WeightedPoint>();

            loadFromPlayerPrefs();
        }

        public void reset()
        {
            points.Clear();
            //Emgu.CV.CvInvoke.cvZero(thresh.Ptr);
        }

        public float getMarkerMin(int index)
        {
            return markerMin[index];
        }

        public float getMarkerMax(int index)
        {
            return markerMax[index];
        }

        public void setMarkerMin(int index, float val)
        {
            if (markerMin[index] != val)
            {
                markerMin[index] = val;
                updateMinMarker();
            }
        }

        public void setMarkerMax(int index, float val)
        {
            if (markerMax[index] != val)
            {
                markerMax[index] = val;
                updateMaxMarker();
            }
        }

        public void updateMinMarker()
        {
            markerMinScalar = new MCvScalar(markerMin[0], markerMin[1], markerMin[2]);
        }

        public void updateMaxMarker()
        {
            markerMaxScalar = new MCvScalar(markerMax[0], markerMax[1], markerMax[2]);
        }

        public void setMarkerMin(float h, float s, float v)
        {
            markerMin[0] = h;
            markerMin[1] = s;
            markerMin[2] = v;
            markerMinScalar = new MCvScalar(h, s, v);

        }

        public void setMarkerMax(float h, float s, float v)
        {
            markerMax[0] = h;
            markerMax[1] = s;
            markerMax[2] = v;
            markerMaxScalar = new MCvScalar(h, s, v);
        }

        public override String ToString()
        {
            return "Lower limit: " + markerMin[0] + " " + markerMin[1] + " " + markerMin[2] + "/n" +
                "upper limit: " + markerMax[0] + " " + markerMax[1] + " " + markerMax[2];
        }

        public struct WeightedPoint
        {
            public int X, Y;
            public float weight;
            public PointF point;

            public WeightedPoint(int x, int y, float weight)
            {
                this.X = x;
                this.Y = y;
                this.weight = weight;
                point = new PointF(x, y);
            }
        }
    }
}
