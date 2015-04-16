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
using Assets.Reality.Scripts.Keyboard;

public class MarkerTracker : AbstractImageProcessor
{
    public enum conditions
    {
        no_blending_user, no_blending_inferred,
        minimal_blending_user, minimal_blending_inferred,
        partial_blending_user, partial_blending_inferred,
        full_blending_user, full_blending_inferred,
        full_blending_gaze, 
        partial_blending_user_green, partial_blending_inferred_green, baseline_typing
    }

    // vars for handling which condition, and what state the condition is in
    public static conditions condition = conditions.minimal_blending_inferred;
    bool toggleMode = true; // toggle whether in reality mode or not

    // for marker detection
    bool drawDebug = false;
    Image<Hsv, Byte> HSVImage;
    int alpha = 255;
    public static int alphaChangePerUpdate = 15;
    int handsPresent = 0;

    // marker vars
    private int x, y;

    // markers
    Marker handMarker = new Marker("handmarker");
    Marker objectMarker = new Marker("objectmarker"); // red
    Marker objectMarkerTwo = new Marker("objectmarkertwo");  // red if we need wrap around
    Marker debugWhichMarker = null;

    // marker HSV gui
    bool drawGUI;
    StructuringElementEx kernel;
    int an = 6;
    MemStorage storage;

    public MarkerTracker(int x, int y, bool debug = false, int contourArea = 30, bool drawGUI = true)
        : base(x, y)
    {
        this.drawDebug = debug;
        this.handContourAreaThreshold = contourArea;
        this.greenScreenContourAreaThreshhold = contourArea;

        HSVImage = new Image<Hsv, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
        retainGreenThreshold = new Image<Gray, byte>(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
        this.drawGUI = false;
        storage = new MemStorage();

        kernel = new StructuringElementEx(an + 1, an + 1, an / 2, an / 2, CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE);

        handContourAreaThreshold = PlayerPrefs.GetFloat("handContourAreaThreshold");
        greenScreenContourAreaThreshhold = PlayerPrefs.GetFloat("greenScreenContourAreaThreshhold");
    }

    int circleArea = 10;
    float approxPolyVar = 0;

    List<Marker.WeightedPoint> lastKnownHands = new List<Marker.WeightedPoint>();

    int handFix = 0;

    public void doHandTracking(Marker marker)
    {
        // threshhold by marker colour
        markerThreshold(HSVImage, marker, marker.thresh);
        
        // now lets find location of hands
        getMarkers(marker, marker.thresh, null, true, null, 0, handContourAreaThreshold, false);

        if (marker.points.Count > 0)
        {
            lastKnownHands.Clear();
            foreach (Marker.WeightedPoint p in marker.points)
            {
                lastKnownHands.Add(p);
            }
        }
        
        Emgu.CV.CvInvoke.cvZero(marker.thresh.Ptr);
        Emgu.CV.CvInvoke.cvZero(marker.bounds.Ptr);

        if ((condition == conditions.partial_blending_user && marker.points.Count == 0))
            handFix++;
        else
            handFix = 0;

        // we retain last known hands, so we can gracefully fade stuff out when the hands disappear
        foreach (Marker.WeightedPoint p in lastKnownHands)
        {
            // maintain a ratio of 12: 7
            //
            Ellipse fadingEllipse = new Ellipse(new PointF(p.X, p.Y+50), new SizeF(190 + (float)(0.3 * p.weight), 90 + (float)(0.2 * p.weight)), 0);
            //Ellipse maskingEllipse = new Ellipse(new PointF(p.X, p.Y + 50), new SizeF(100 + (float)(0.2 * p.weight), 45 + (float)(0.2 * p.weight)), 0);

            // this we use for adding the hands to the green contour to fix occlusions
            if (handFix < 4)
                marker.thresh.Draw(fadingEllipse, new Gray(255), -1);

            // this we use as the alpha
            marker.bounds.Draw(fadingEllipse, new Gray(alpha), -1);
        }
        

        marker.bounds._SmoothGaussian(47);

        //Debug.Log("Hands: " + marker.points.Count);
        handsPresent = marker.points.Count;
    }

    Image<Gray, byte> retainGreenThreshold;
    List<Contour<Point>> contours = new List<Contour<Point>>();

    public void doObjectTracking(Marker marker, Marker handsMarker, bool permanentObjects, bool shouldTheyBeInGreenContour)
    {
        //Threshold the green screen
        markerThreshold(HSVImage, marker, marker.thresh);

        /*
         * PARTIAL BLENDING
         */
        if (shouldTheyBeInGreenContour)
        {
            // and keep the green threshold about, cos we'll need it at the end
            // in order to remove the green around the hands
            CvInvoke.cvCopy(marker.thresh.Ptr, retainGreenThreshold.Ptr, IntPtr.Zero);

            // HANDS trick pt1: make the green thresh image think the hands are green
            // this is so that a hand cannot break our complete green contour
            CvInvoke.cvOr(marker.thresh.Ptr, handsMarker.thresh.Ptr, marker.thresh.Ptr, IntPtr.Zero);

            // generate our greenscreen contour
            getMarkers(marker, marker.thresh, marker.bounds, false, null, 0, greenScreenContourAreaThreshhold, true);

            //invert our threshold image
            CvInvoke.cvNot(marker.thresh.Ptr, marker.thresh.Ptr); // this one has hand circles included
            CvInvoke.cvNot(retainGreenThreshold.Ptr, retainGreenThreshold.Ptr); // this one doesn't (real green screen)

            // HANDS trick pt2 - now make the hands appear to be objects within the green contour
            CvInvoke.cvOr(marker.thresh.Ptr, handsMarker.thresh.Ptr, marker.thresh.Ptr, IntPtr.Zero);

            //AND the masks to get only the objects within the green contour
            CvInvoke.cvAnd(marker.bounds.Ptr, marker.thresh.Ptr, marker.bounds.Ptr, IntPtr.Zero);

            // at this point we have a mask of stuff that was within the green contour plus circles around hand markers

            if (permanentObjects)
            {
                marker.bounds.SetValue(alpha, marker.bounds);
            }
            else // NEVER HAPPENS?
            {
                // take our fuzzy circles at hand marker locations, and copy them using our actual objects as a mask
                CvInvoke.cvCopy(handsMarker.bounds.Ptr, marker.bounds.Ptr, marker.bounds.Ptr);
            }

            // then exclude anything green (for the area around our hands)
            CvInvoke.cvAnd(marker.bounds.Ptr, retainGreenThreshold.Ptr, marker.bounds.Ptr, IntPtr.Zero);
            //getMarkers(marker, marker.bounds, marker.bounds, false, null, 0, handContourAreaThreshold);
            marker.bounds._SmoothGaussian(7);
        }
        else
        {
            if (permanentObjects)
            {
                /*
                 * PARTIAL BLENDING GREEN (FOR TYPING STUDY)
                 */
                // just green threshhold, nothing else 
                CvInvoke.cvNot(marker.thresh.Ptr, marker.thresh.Ptr); // this one has hand circles included
                CvInvoke.cvCopy(marker.thresh.Ptr, marker.bounds.Ptr, IntPtr.Zero);
                marker.bounds._SmoothGaussian(5);
                marker.bounds._Erode(1);
            }
            else
            {
                /*
                 * MINIMAL BLENDING (HANDS)
                 */
                CvInvoke.cvZero(marker.bounds.Ptr);
                CvInvoke.cvNot(marker.thresh.Ptr, marker.thresh.Ptr); // this one has hand circles included
                getMarkers(marker, marker.thresh, marker.thresh, true, null, 0, handContourAreaThreshold, false);

                CvInvoke.cvCopy(handsMarker.bounds.Ptr, marker.bounds.Ptr, marker.thresh.Ptr);
            }
        }

    }



    public override void processImage(Emgu.CV.Image<Rgba, byte> cvColorImage, UnityEngine.Color[] alphaMask)
    {

        // convert to HSV
        Emgu.CV.CvInvoke.cvCvtColor(cvColorImage.Ptr, HSVImage.Ptr, Emgu.CV.CvEnum.COLOR_CONVERSION.BGR2HSV);
        HSVImage._SmoothGaussian(5);

        if (drawDebug) cvColorImage.Save("cvProcessImage.jpg");
        if (drawDebug) HSVImage.Save("cvHSVImage.jpg");

        // render based on our condition
        if (selectedDebugMarker > 0) // bypass conditions for debug
        {
            markerThreshold(HSVImage, debugWhichMarker, debugWhichMarker.thresh);
            imageCopier.parallelCopy(debugWhichMarker.thresh, alphaMask);
        }
        else
        {
            switch (condition)
            {
                case conditions.minimal_blending_user:
                case conditions.minimal_blending_inferred:
                    {
                        doHandTracking(handMarker);
                        break;
                    }
                case conditions.partial_blending_inferred:
                case conditions.partial_blending_user:
                case conditions.full_blending_user:
                case conditions.full_blending_inferred:
                case conditions.partial_blending_user_green:
                case conditions.partial_blending_inferred_green:
                    {
                        doHandTracking(handMarker);
                        break;
                    }
            }

            // if reality is enabled or we can see the hands, 
            // decrease the alpha
            switch (condition)
            {
                case conditions.minimal_blending_user:
                    {
                        if (toggleMode)
                        {
                            if (!(handsPresent > 0))
                            {
                                alpha -= alphaChangePerUpdate;
                                if (alpha < 0) alpha = 0;
                            }
                            // otherwise increase the alpha
                            else
                            {
                                alpha += alphaChangePerUpdate;
                                if (alpha > 255) alpha = 255;
                            }
                        }
                        else
                        {
                            alpha -= alphaChangePerUpdate;
                            if (alpha < 0) alpha = 0;
                        }
                        break;
                    }
                case conditions.partial_blending_user:
                case conditions.full_blending_user:
                case conditions.partial_blending_user_green:
                    {
                        if (!toggleMode)
                        {
                            alpha -= alphaChangePerUpdate;
                            if (alpha < 0) alpha = 0;
                        }
                        // otherwise increase the alpha
                        else
                        {
                            alpha += alphaChangePerUpdate;
                            if (alpha > 255) alpha = 255;
                        }
                        break;
                    }
                case conditions.minimal_blending_inferred:
                case conditions.partial_blending_inferred:
                case conditions.full_blending_inferred:
                case conditions.partial_blending_inferred_green:
                    {
                        if (! (handsPresent > 0))
                        {
                            alpha -= alphaChangePerUpdate;
                            if (alpha < 0) alpha = 0;
                        }
                        // otherwise increase the alpha
                        else
                        {
                            alpha += alphaChangePerUpdate;
                            if (alpha > 255) alpha = 255;
                        }
                        break;
                    }
                case conditions.full_blending_gaze:
                    {
                        if (RiftAngle.cameraAngle > 5 && RiftAngle.cameraAngle < RiftAngle.keyboardAngle)
                        {
                            alpha = (int)((255 / RiftAngle.keyboardAngle) * (RiftAngle.cameraAngle - 5));
                        }
                        else if (RiftAngle.cameraAngle > RiftAngle.keyboardAngle && RiftAngle.cameraAngle < 90)
                        {
                            alpha = 255;
                        }
                        else
                        {
                            alpha = 0;
                        }

                        break;
                    }
            }

            switch (condition)
            {
                case conditions.no_blending_user:
                case conditions.no_blending_inferred:
                case conditions.baseline_typing:
                    {
                        Emgu.CV.CvInvoke.cvZero(cvMask.Ptr);
                        imageCopier.parallelCopy(cvMask, alphaMask);

                        break;
                    }
                case conditions.minimal_blending_user:
                case conditions.minimal_blending_inferred:
                    {
                        doObjectTracking(objectMarker, handMarker, false, false);
                        imageCopier.parallelCopy(objectMarker.bounds, alphaMask);
                        break;
                    }
                case conditions.partial_blending_user:
                case conditions.partial_blending_inferred:
                    {
                        doObjectTracking(objectMarker, handMarker, true, true);
                        imageCopier.parallelCopy(objectMarker.bounds, alphaMask);
                        break;
                    }
                case conditions.partial_blending_user_green:
                case conditions.partial_blending_inferred_green:
                    {
                        doObjectTracking(objectMarker, handMarker, true, false);
                        Emgu.CV.CvInvoke.cvSet(objectMarker.bounds.Ptr, new MCvScalar(alpha), objectMarker.bounds.Ptr);
                        objectMarker.bounds._SmoothGaussian(5);
                        imageCopier.parallelCopy(objectMarker.bounds, alphaMask);
                        break;
                    }
                case conditions.full_blending_user:
                case conditions.full_blending_inferred:
                case conditions.full_blending_gaze:
                    {
                        Emgu.CV.CvInvoke.cvSet(cvMask.Ptr, new MCvScalar(alpha), IntPtr.Zero);
                        imageCopier.parallelCopy(cvMask, alphaMask);

                        break;
                    }
            }
        }
    }

    void markerThreshold(Image<Hsv, byte> HSVImage, Marker marker, Marker markerTwo)
    {
        markerThreshold(HSVImage, marker, marker.thresh);
        markerThreshold(HSVImage, markerTwo, marker.thresh);
        if (drawDebug)
        {
            marker.thresh.Save("cvMark.jpg");
            markerTwo.thresh.Save("cvMark2.jpg");
        }
        Emgu.CV.CvInvoke.cvOr(marker.thresh, markerTwo.thresh, marker.thresh, IntPtr.Zero);
        if (drawDebug) marker.thresh.Save("cvMarkOr.jpg");
    }

    void markerThreshold(Image<Hsv, byte> HSVImage, Marker marker, Image<Gray, byte> image)
    {
        Emgu.CV.CvInvoke.cvInRangeS(HSVImage.Ptr, marker.markerMinScalar, marker.markerMaxScalar, image.Ptr);
    }

    float handContourAreaThreshold, greenScreenContourAreaThreshhold;
    bool found = false, doOnce = false;

    void getMarkers(Marker marker, Image<Gray, byte> imageToFindContours, Image<Gray, byte> imageToDrawContours, bool findPoints, List<Contour<Point>> matchingContours, int approxPoly, float contourAreaThreshold, bool drawContourFix)
    {
        if (findPoints)
            marker.points.Clear();

        doOnce = imageToDrawContours != null ? true : false;

        for (Contour<Point> contours = imageToFindContours.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
        {
            if (doOnce)
            {
                // clear our image as we are going to redraw the contours
                Emgu.CV.CvInvoke.cvZero(imageToDrawContours.Ptr);
                doOnce = false;
            }
            if (contours.Area > contourAreaThreshold)
            {
                x = contours.BoundingRectangle.Left + contours.BoundingRectangle.Width / 2;
                y = contours.BoundingRectangle.Top + contours.BoundingRectangle.Height / 2;

                if (findPoints)
                    marker.points.Add(new Marker.WeightedPoint(x, y, (float)contours.Area));

                if (imageToDrawContours != null)
                {
                    if (approxPoly > 0)
                    {
                        imageToDrawContours.Draw(contours.ApproxPoly(approxPoly), new Gray(alpha), -1);
                    }
                    else
                    {
                        imageToDrawContours.Draw(contours, new Gray(alpha), -1);
                    }
                }

                if (matchingContours != null) {
                    if (approxPoly > 0)
                    {
                        matchingContours.Add(contours.ApproxPoly(approxPoly));
                    }
                    else
                    {
                        matchingContours.Add(contours);
                    }
                }
            }
        }

        storage.Clear();

        //if (drawDebug)
        //    if (marker.points.Count > 0)
        //        Debug.Log("found points: " + marker.points.Count);
        //    else
        //        Debug.Log("no points :( ");
    }

    public static bool disableConditionKeys = true;
    public static bool disableGamepadAndMiscCtrls = true;
    public bool newCondition = false;

    public override void handleKeyPresses()
    {
        if (!disableConditionKeys)
        {
            // condition handling
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                condition = conditions.no_blending_inferred;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha2))
            {
                condition = conditions.no_blending_user;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha3))
            {
                condition = conditions.minimal_blending_inferred;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha4))
            {
                condition = conditions.minimal_blending_user;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5))
            {
                condition = conditions.partial_blending_inferred;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha6))
            {
                condition = conditions.partial_blending_user;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha7))
            {
                condition = conditions.full_blending_inferred;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha8))
            {
                condition = conditions.full_blending_user;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha9))
            {
                condition = conditions.full_blending_gaze;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha0))
            {
                condition = conditions.partial_blending_inferred_green;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Minus))
            {
                condition = conditions.partial_blending_user_green;
                TextMeshManager.showTextForPeriod("" + condition, 2);
                newCondition = true;

            }
        }

        bool isStart = false;
        bool isEnd = false;

        if (!disableConditionKeys)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (newCondition)
                {
                    isStart = true;
                    newCondition = false;
                } else {
                    isEnd = true;
                }
            }
        }        

        // only look for these keys when not doing typing study!
        if (!disableGamepadAndMiscCtrls)
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
                TextMeshManager.showTextForPeriod("Interact with reality", 2);

            // toggle reality on and offf
            if (Input.GetButtonDown("Fire1") ||
                Input.GetButtonDown("Fire2") ||
                Input.GetButtonDown("Fire3") ||
                Input.GetButtonDown("Jump"))
            {
                toggleMode = !toggleMode;
                Debug.Log("Toggling reality to " + toggleMode + " (Gamepad button pressed)");
            }
        }

        logReality(isStart, isEnd);

        if (false && !disableGamepadAndMiscCtrls)
        {
            // save current marker settings
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            {
                // save our markers
                handMarker.saveToPlayerPrefs();
                objectMarker.saveToPlayerPrefs();
                objectMarkerTwo.saveToPlayerPrefs();

                PlayerPrefs.SetFloat("handContourAreaThreshold", handContourAreaThreshold);
                PlayerPrefs.SetFloat("greenScreenContourAreaThreshhold", greenScreenContourAreaThreshhold);
            }
        }
    }

    bool isRealityOn = false;
    
    public void logReality(bool start, bool end)
    {
        bool currentReality = false;

        if (condition == conditions.full_blending_inferred ||
            condition == conditions.minimal_blending_inferred ||
            condition == conditions.partial_blending_inferred)
        {
            if (handsPresent > 0)
                currentReality = true;
        }

        if (condition == conditions.full_blending_user ||
            condition == conditions.minimal_blending_user ||
            condition == conditions.partial_blending_user)
        {
            if (toggleMode)
                currentReality = true;
        }

        //Debug.Log("current " + currentReality + " prev " + isRealityOn);
        if (start)
        {
            LogWriter.AmountOfReality.WriteToLog("," + Environment.TickCount + ", started, " + currentReality + ", " + condition);
        }
        else if (end)
        {
            LogWriter.AmountOfReality.WriteToLog("," + Environment.TickCount + ", finished, " + currentReality + ", " + condition);
        }
        else if (currentReality && !isRealityOn)
        {
            LogWriter.AmountOfReality.WriteToLog("," + Environment.TickCount + ", toggledon, " + currentReality + ", " + condition);
        }
        else if (!currentReality && isRealityOn)
        {
            LogWriter.AmountOfReality.WriteToLog("," + Environment.TickCount + ", toggledoff, " + currentReality + ", " + condition);
        }

        isRealityOn = currentReality;
    }

    public static void setCondition(int cond)
    {
        switch (cond)
        {
            case 1:
                {
                    condition = conditions.no_blending_inferred;
                    break;
                }
            case 2:
                {
                    condition = conditions.no_blending_user;
                    break;
                }
            case 3: {
                condition = conditions.minimal_blending_inferred;
                break;
            }
            case 4:{
                condition = conditions.minimal_blending_user;
                break;
            }
            case 5:{
                condition = conditions.partial_blending_inferred;
                break;
            }
            case 6:{
                condition = conditions.partial_blending_user;
                break;
            }
            case 7:{
                condition = conditions.full_blending_inferred;
                break;
            }
            case 8:{
                condition = conditions.full_blending_user;
                break;
            } 
            case 9:{
                condition = conditions.full_blending_gaze;
                break;
            }
            case 10: {
                condition = conditions.partial_blending_inferred_green;
                break;
            }
            case 11:{
                condition = conditions.partial_blending_user_green;
                break;
            }
            case 12:{
                condition = conditions.baseline_typing;
                break;
            }
        }
        Debug.Log("Condition set to " + condition);
    }
    int selectedDebugMarker = 0;
    string[] options = new String[] { "None", "Draw hand thresh", "Draw object thresh one", "Draw object thresh two" };
    public override void OnGUI()
    {
        if (drawGUI)
        {
            selectedDebugMarker = GUILayout.SelectionGrid(selectedDebugMarker, options, 2);

            switch (selectedDebugMarker)
            {
                case 0: debugWhichMarker = null; break;
                case 1: debugWhichMarker = handMarker; break;
                case 2: debugWhichMarker = objectMarker; break;
                case 3: debugWhichMarker = objectMarkerTwo; break;
            }

            if (debugWhichMarker != null)
            {
                GUILayout.Label("Hue");
                debugWhichMarker.setMarkerMin(0, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMin(0), 0, 255));
                debugWhichMarker.setMarkerMax(0, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMax(0), 0, 255));
                GUILayout.Label("Sat");
                debugWhichMarker.setMarkerMin(1, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMin(1), 0, 255));
                debugWhichMarker.setMarkerMax(1, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMax(1), 0, 255));
                GUILayout.Label("Bri");
                debugWhichMarker.setMarkerMin(2, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMin(2), 0, 255));
                debugWhichMarker.setMarkerMax(2, GUILayout.HorizontalSlider(debugWhichMarker.getMarkerMax(2), 0, 255));
            }

            
            GUILayout.Label("handContourAreaThreshold");
            handContourAreaThreshold = GUILayout.HorizontalSlider(handContourAreaThreshold, 1, 300);
            GUILayout.Label("greenScreenContourAreaThreshhold");
            greenScreenContourAreaThreshhold = GUILayout.HorizontalSlider(greenScreenContourAreaThreshhold, 1, 1000);
            GUILayout.Label("approxPolyVar");
            approxPolyVar = GUILayout.HorizontalSlider(approxPolyVar, 0, 300);
        }
    }
}


