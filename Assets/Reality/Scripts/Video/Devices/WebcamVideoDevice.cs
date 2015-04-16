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

namespace Assets.KinectScripts.Cameras
{
    class WebcamVideoDevice : AbstractVideoDevice
    {
        public WebCamTexture webcamTexture;
		ParallelSubSampler subSampler;

        public WebcamVideoDevice(int x, int y, int subSampleRatio, Renderer renderer)
            : base(renderer)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            string camLogiC310 = "";

            UnityEngine.Debug.Log("Webcams detected:");
            foreach (WebCamDevice device in devices)
            {
                UnityEngine.Debug.Log("" + device.name + " " + device.ToString());
                if (device.name.Contains("Logitech HD"))
                {
                    camLogiC310 = device.name;
                }

            }

			if (camLogiC310.Length > 0){
				webcamTexture = new WebCamTexture(camLogiC310, x, y, 30);
			} else {
				Debug.LogError("Can't find a C310, using first webcam we get..");
				webcamTexture = new WebCamTexture(x, y, 30);
                Debug.LogError("Webcam is " + webcamTexture.deviceName);
			}

            webcamTexture.Play();
            UnityEngine.Debug.Log("Actual webcam settings we got: " + webcamTexture.height + " * " + webcamTexture.width + " fps " + webcamTexture.requestedFPS);
            this.x = webcamTexture.width;
            this.y = webcamTexture.height;
            noOfPixels = x * y;
            renderPlainRenderer.material.SetTexture("_MainTex", webcamTexture);
			subSampler = new ParallelSubSampler();
        }

        // On the Unity thread, we want to see if the webcamTexture
        // has a new frame - if it does, we want to get it's pixels
        // (as we can only do that on Unity thread), then pass
        // the pixels off to another thread to convert to emguCV..
        public override bool UpdateImageBuffers(){
            if (webcamTexture.didUpdateThisFrame)
            {
				newFrames++;

				// hand the task of copying/downsampling the image to another thread
				processing_singleThread(webcamTexture.GetPixels32());

				// instrumentation (works for both methods)
				if (Environment.TickCount - fpsTime > 1000){
					Debug.Log("Downsampling webcam image at FPS: " + fps + "; camera framerate is " + newFrames);
					fpsTime = Environment.TickCount;
					fps = 0;
					newFrames = 0;
				}
                return true;
            }
            return false;
        }

		Color32[] colorImage;
		Thread subsamplingThread = null;
		String lockObject = "lol";
		AutoResetEvent waitHandle;
		int fps = 0;
		int newFrames = 0;
		long fpsTime = 0;

		void processing_singleThread(Color32[] colourImage){
			// we're going to have a thread that sleeps, waiting until theres a new image
			// it then copies that image and notifies prcessor

			if (subsamplingThread == null){
				UnityEngine.Debug.Log("WebcamVideoDevice: Creating thread");
				subsamplingThread = new Thread(CopyC32toCVThreadRunnable);
				subsamplingThread.Priority = System.Threading.ThreadPriority.Highest;
				waitHandle = new AutoResetEvent(false);
				subsamplingThread.Start();
			}
			this.colorImage = colourImage;
			waitHandle.Set();
			
		}

		void CopyC32toCVThreadRunnable(){
			while (lockObject != null){
				waitHandle.WaitOne();
				CopyC32toCV( colorImage );
			}
		}
		
		void CopyC32toCV(Color32[] imageToCopy)
        {
            try
            {			
                DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>>.display buffer = colourImagesBuffer.getOldestDisplay();
				buffer.id = frameID;

				// we can either downsample using one thread, the thread
				// that called this method..
				//subSampler.subSampleC32toCVSingleThreaded(buffer.getDisplay(), imageToCopy, x, y);

				// or we can split the task up across multiple threads and this thread 
				// simply waits until they are all done..
				subSampler.parallelSubSampleC32toCV(buffer.getDisplay(), imageToCopy, x, y);

                buffer.hasBeenDisplayed = false;
                colourImagesBuffer.releaseLock(buffer, true);

                if (toBeNotified != null)
                {
                    toBeNotified.Set();
                }
				fps++;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public override void OnApplicationQuit(){
			lockObject = null;
		}

    }
}
