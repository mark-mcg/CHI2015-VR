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
using Assets.Scripts.Processors;

public class VideoDeviceProcessor {
    public AbstractImageProcessor processor;
    public AbstractVideoDevice video;

	private Thread processingThread;
	private String lockObject = "lock";
    private DisplayBuffer<UnityEngine.Color[]> maskImagesBuffer;

    private AutoResetEvent waitHandle;

	private int fps = 0;
	private long fpsTime = 0;

	// Use this for initialization
	public VideoDeviceProcessor (AbstractVideoDevice videoToProcess) { 
        video = videoToProcess;

        // initialise our render buffer
        maskImagesBuffer = DisplayBuffer<UnityEngine.Color[]>.DisplayBufferFactoryColor(AbstractVideoDevice.cvHeight * AbstractVideoDevice.cvWidth);
        
        // what are we going to process the image with?
        //processor = new TestProcessor(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight);
        processor = new MarkerTracker(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight, false);
        //processor = new MarkerTrackerDanny(AbstractVideoDevice.cvWidth, AbstractVideoDevice.cvHeight, false);

		// setup our processing thread
		UnityEngine.Debug.Log("VideoDeviceProcessor: Creating thread");
		processingThread = new Thread(processingThreadRunnable);
		processingThread.Priority = System.Threading.ThreadPriority.Highest;
		waitHandle = new AutoResetEvent(false);
		video.notifyOnNewFrame(waitHandle);
		processingThread.Start();
	}

    public void Update()
    {
        try
        {
            handleKeyPresses();
        }
        catch (Exception ex)
        { 
            Debug.LogError("VideoDeviceProcessor" + ex.ToString());
        }
    }

	private void processingThreadRunnable() {
        UnityEngine.Debug.Log("VideoDeviceProcessor: Starting thread");

		while (lockObject != null){
            try
            {
				// get an old mask we can write to
                DisplayBuffer<UnityEngine.Color[]>.display currentMask = maskImagesBuffer.getOldestDisplay();

				// get the latest downscaled colour image
                DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>>.display currentColourImage = video.getColourImagesBuffer().getNewestDisplay();

				// process the images
                if (processor != null && !currentColourImage.hasBeenDisplayed)
                   processor.processImage(currentColourImage.getDisplay(), currentMask.getDisplay());

				// mark the colour image as having been processed
				// (so we dont process it again)
                currentColourImage.hasBeenDisplayed = true;

				// mark our mask as new
                currentMask.hasBeenDisplayed = false;
				currentMask.id = currentColourImage.id;

				// release both images so they can be displayed or re-used
                maskImagesBuffer.releaseLock(currentMask, true);
				video.getColourImagesBuffer().releaseLock(currentColourImage, false);


				// instrumentation
				fps++;
				if (Environment.TickCount - fpsTime > 1000){
					Debug.Log("VideoDeviceProcessor FPS: " + fps );
					fpsTime = Environment.TickCount;
					fps = 0;
				}

                // wait until we are notified of a new colour image..
                waitHandle.WaitOne();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("VideoDeviceProcessor: Exception in thread " + e);
            }
		}
        UnityEngine.Debug.Log("VideoDeviceProcessor: finished thread");
	}

    /*
     * Handle just the mask
     * 
     * We try to avoid re-uploading the mask if it hasn't changed..
     */
    DisplayBuffer<UnityEngine.Color[]>.display maskBeingRendered;

    public bool isNewMaskAvailable()
    {
        maskBeingRendered = maskImagesBuffer.getNewestDisplay();
        return !maskBeingRendered.hasBeenDisplayed;
    }

    public UnityEngine.Color[] getMask()
    {
        maskBeingRendered.hasBeenDisplayed = true;
        return maskBeingRendered.getDisplay();
    }

	public int getMaskID()
	{
		return maskBeingRendered.id;
	}

    public void finishWithMask(){
        maskImagesBuffer.releaseLock(maskBeingRendered, false);
    }


    public void handleKeyPresses()
    {
        if (processor != null)
            processor.handleKeyPresses();
    }

	public void OnGUI(){
		if (processor != null){
			processor.OnGUI();
		}
	}

    // Make sure to kill the Kinect on quitting.
    public void OnApplicationQuit()
    {
        // kill our threads
        lockObject = null;
		if (processor != null){
			processor.OnApplicationQuit();
		}
    }
}
