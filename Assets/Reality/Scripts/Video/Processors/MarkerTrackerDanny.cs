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

public class MarkerTrackerDanny : AbstractImageProcessor
{
    // for marker detection
    bool drawDebug = false;
    int gaussianSmoothing = 3;
    bool hideMarkers = false;
    Image<Hsv, Byte> HSVImage;
    Image<Gray, byte> HSVThresh;

    // for drawing locations of markers
    List<Point> points = new List<Point>();
    private int x, y;
    int circleRadius = 50;
    int circleGaussianSmoothing = 43;
    int circleAlpha = 255;
    int circleOffsetY = 60;

    // markers
    MCvScalar lowerLimitScalar;
    MCvScalar upperLimitScalar;
    float[] hsvMax = new float[3];
    float[] hsvMin = new float[3];
    bool drawGUI = false;

    int contourAreaThreshold;

    public MarkerTrackerDanny(int x, int y, bool debug = false, int contourArea = 15, bool drawGUI = true)
        : base(x, y)
    {
        this.drawDebug = debug;
        this.contourAreaThreshold = contourArea;

        HSVImage = new Image<Hsv, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
        HSVThresh = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);

        setMarkersRed();
        this.drawGUI = drawGUI;
    }

    public override void processImage(Emgu.CV.Image<Rgba, byte> cvColorImage, UnityEngine.Color[] alphaMask)
    {
        // smooth our image
        //cvColorImage._SmoothGaussian(gaussianSmoothing);
        if (drawDebug) cvColorImage.Save("cvProcessImage.jpg");

        // convert to HSV
        Emgu.CV.CvInvoke.cvCvtColor(cvColorImage.Ptr, HSVImage.Ptr, Emgu.CV.CvEnum.COLOR_CONVERSION.BGR2HSV);
        if (drawDebug) HSVImage.Save("cvHSVImage.jpg");

        // threshhold
        Emgu.CV.CvInvoke.cvInRangeS(HSVImage.Ptr, lowerLimitScalar, upperLimitScalar, HSVThresh.Ptr);
        if (drawDebug) HSVThresh.Save("cvHSVThresh.jpg");
        //HSVThresh._Dilate(50);
        //HSVThresh._Erode(50);
        //HSVThresh._Dilate(50);
        //HSVThresh.SmoothMedian(7);
        //HSVThresh._Erode(1);
        if (drawDebug) HSVThresh.Save("cvHSVThreshDilated.jpg");

        /*Emgu.CV.Cvb.CvBlobs resultingBlobs = new Emgu.CV.Cvb.CvBlobs(); 
        Emgu.CV.Cvb.CvBlobDetector bDetect = new Emgu.CV.Cvb.CvBlobDetector(); 
        uint numBlobsFound = bDetect.Detect(HSVThresh, resultingBlobs); 
        Image<Bgr, Byte> blobImg = bDetect.DrawBlobs(HSVThresh, resultingBlobs, Emgu.CV.Cvb.CvBlobDetector.BlobRenderType.Default, 0.5); 
        if (drawDebug) blobImg.Save("cvBlobsImage.jpg");*/

        // find markers
        points.Clear();
        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
            for (Contour<Point> contours = HSVThresh.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
            {
                if (contours.Area > contourAreaThreshold)
                {
                    //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                    x = contours.BoundingRectangle.Left + contours.BoundingRectangle.Width / 2;
                    y = contours.BoundingRectangle.Top + contours.BoundingRectangle.Height / 2;

                    //cvHSVThresh.Draw(contours, new Gray(128), -1);
                    HSVThresh.Draw(new CircleF(new PointF(x, y), 3), new Gray(64), -1);
                    points.Add(new Point(x, y));
                }
            }
        if (drawDebug) HSVThresh.Save("cvHSVThreshWithContourCenters.jpg");
        ;
        // draw our mask using marker locations
        Emgu.CV.CvInvoke.cvZero(cvMask.Ptr);

        if (drawDebug)
            if (points.Count > 0)
                Debug.Log("found points: " + points.Count);
            else
                Debug.Log("no points :( ");

        foreach (Point p in points)
        {
            cvMask.Draw(new CircleF(new PointF(p.X, p.Y), circleRadius), new Gray(circleAlpha), -1);
        }

        // smooth mask off aggresively for a nice feather effect
        //cvMask._SmoothGaussian(circleGaussianSmoothing);


        // dont wnat a mask atm, disable
        //Emgu.CV.CvInvoke.cvZero(cvMask.Ptr);
        HSVThresh.Copy(cvMask);
        HSVThresh._Dilate(50);
        HSVThresh._Erode(50);

        points.Clear();
        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
            for (Contour<Point> contours = HSVThresh.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
            {
                if (contours.Area > 50 * contourAreaThreshold)
                {
                    //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                    x = contours.BoundingRectangle.Left + contours.BoundingRectangle.Width / 2;
                    y = contours.BoundingRectangle.Top + contours.BoundingRectangle.Height / 2;

                    //cvHSVThresh.Draw(contours, new Gray(128), -1);
                    //HSVThresh.Draw(new CircleF(new PointF(x, y), 3), new Gray(64), -1);
                    points.Add(new Point(x, y));
                }
            }

        foreach (Point p in points)
        {
            cvMask.Draw(new CircleF(new PointF(p.X, p.Y), 2 * circleRadius), new Gray(circleAlpha), -1);
        }

        // copy CV mask to unity Color[] for uploading to GPU
        imageCopier.parallelCopy(cvMask/*HSVThresh*/, alphaMask);
    }


    // just does red atm (crappily)
    void setMarkersRed()
    {
        hsvMin[0] = 150;
        hsvMin[1] = 150;
        hsvMin[2] = 0;

        hsvMax[0] = 200;
        hsvMax[1] = 200;
        hsvMax[2] = 255;

        lowerLimitScalar = new MCvScalar(hsvMin[0], hsvMin[1], hsvMin[2]);
        upperLimitScalar = new MCvScalar(hsvMax[0], hsvMax[1], hsvMax[2]);
    }

    void setMarkers()
    {
        hsvMin[0] = 0;
        hsvMin[1] = 0;
        hsvMin[2] = 0;

        hsvMax[0] = 255;
        hsvMax[1] = 255;
        hsvMax[2] = 255;

        lowerLimitScalar = new MCvScalar(hsvMin[0], hsvMin[1], hsvMin[2]);
        upperLimitScalar = new MCvScalar(hsvMax[0], hsvMax[1], hsvMax[2]);
    }



    #region Old marker stuff
    // HSV in open cv is H: 0 - 180, S: 0 - 255, V: 0 - 255
    // HSV in photoshop is H = 0-360, S = 0-100 and V = 0-100
    Hsv createHSVfromPhotoshopHSV(int h, int s, int v)
    {
        UnityEngine.Debug.Log("hsv " + h + " " + s + " " + v);
        h = (int)(h - 5);
        s = (int)(s * 2.55);
        v = (int)(v * 2.55);
        UnityEngine.Debug.Log("hsv mod " + h + " " + s + " " + v);
        return new Hsv(h, s, v);
    }

    // HSV in open cv is H: 0 - 180, S: 0 - 255, V: 0 - 255
    // Matlab is all 0..255
    Hsv createHSVfromMatlabHSV(int h, int s, int v)
    {
        UnityEngine.Debug.Log("hsv " + h + " " + s + " " + v);
        h = (int)(((float)h / 255) * 180);
        UnityEngine.Debug.Log("hsv mod " + h + " " + s + " " + v);
        return new Hsv(h, s, v);
    }

    #endregion

    public override void handleKeyPresses()
    {
        if (Input.GetKeyDown("h"))
        {
            Debug.Log("Lower limit: " + hsvMin[0] + " " + hsvMin[1] + " " + hsvMin[2]);
            Debug.Log("upper limit: " + hsvMax[0] + " " + hsvMax[1] + " " + hsvMax[2]);
        }
    }

    public override void OnGUI()
    {
        if (drawGUI)
        {
            GUILayout.Label("EmguCV HSV Controls");
            GUILayout.Label("Hue");
            hsvMin[0] = GUILayout.HorizontalSlider(hsvMin[0], 0, 255);
            hsvMax[0] = GUILayout.HorizontalSlider(hsvMax[0], 0, 255);
            GUILayout.Label("Sat");
            hsvMin[1] = GUILayout.HorizontalSlider(hsvMin[1], 0, 255);
            hsvMax[1] = GUILayout.HorizontalSlider(hsvMax[1], 0, 255);
            GUILayout.Label("Bri");
            hsvMin[2] = GUILayout.HorizontalSlider(hsvMin[2], 0, 255);
            hsvMax[2] = GUILayout.HorizontalSlider(hsvMax[2], 0, 255);

            lowerLimitScalar = new MCvScalar(hsvMin[0], hsvMin[1], hsvMin[2]);
            upperLimitScalar = new MCvScalar(hsvMax[0], hsvMax[1], hsvMax[2]);
        }
    }
}

