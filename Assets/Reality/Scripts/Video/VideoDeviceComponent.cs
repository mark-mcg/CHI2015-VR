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
using Assets.KinectScripts;
using Assets.KinectScripts.Cameras;

public class VideoDeviceComponent : MonoBehaviour
{

    private AbstractVideoDevice[] videos = new AbstractVideoDevice[1];
    bool Kinect = false; // decides whether we are in people mode or desk mode
    VideoDeviceProcessor vdp;

    // Use this for initialization
    void Awake()
    {
        Debug.Log("VideoDeviceComponent Awake()");

        if (!Kinect)
        {
            // want a 16:10 ratio e.g. 640 * 400, 720 * 450,  1280, 960
            videos[0] = new WebcamVideoDevice(752, 416, 4, renderer);
        }
        else
        {
            // need to rewrite kinect for codebase redesign..
        }

        vdp = new VideoDeviceProcessor(videos[0]);
        Debug.Log("VideoDeviceComponent Awake() finished");

    }

    // This update method predominantly does some house keeping, with it's main load being
    // applying new masks to video devices when available
    // It does *NO* processing outside of this

    // We call it twice every unity update, from Update() and LateUpdate() so we have
    // the freshest mask
	int frameID = 0;
	float frameLag = 0;
	long lastUpdate = 0;
	void UpdateInternal(bool lateUpdate)
    {
        try
        {
			if (lateUpdate){
	            // update our VDP, allowing it to receive key inputs and stuff
	            // (its actual work is on a different thread)
	            vdp.Update();

	            // update our video devices 
				videos[0].setFrame(frameID);
	            videos[0].Update();
				frameID++;
			}

            // for each pair of videomanager and video, apply the mask
            // we check for a new mask in Update and LateUpdate
            if (vdp.isNewMaskAvailable())
            {
				frameLag = (frameLag + (frameID - vdp.getMaskID()) ) / 2;
				if (Environment.TickCount - lastUpdate > 1000){
					Debug.Log("Frame lag is " + frameLag);
					lastUpdate = Environment.TickCount;
				}
	            videos[0].getAlphaTexture().SetPixels(vdp.getMask());
	            videos[0].getAlphaTexture().Apply();
            }
			vdp.finishWithMask();

        }
        catch (Exception ex)
        {
            Debug.LogError("VideoDeviceContainer" + ex.ToString());
        }
    }

    void Update()
    {
        UpdateInternal(false);
    }

	void LateUpdate()
	{
		UpdateInternal(true);
	}

	void OnGUI(){
		if (vdp != null){
			vdp.OnGUI();
		}
	}

    void OnApplicationQuit()
    {
		if (vdp != null){
	        vdp.OnApplicationQuit();
		}
        videos[0].OnApplicationQuit();
    }
}
