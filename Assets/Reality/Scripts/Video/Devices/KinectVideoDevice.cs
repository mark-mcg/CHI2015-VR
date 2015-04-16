// need to redo this at some point..

//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using System.IO;
//using System.Text;
//using System.Drawing;
//using Emgu.CV;
//using Emgu.CV.Util;
//using Emgu.CV.UI;
//using Emgu.CV.CvEnum;
//using Emgu.CV.Structure;
//using System.Threading;

//namespace Assets.KinectScripts
//{
//    public class KinectImage : AbstractImage
//    {
//        short[] usersDepthMap;
//        float[] usersHistogramMap;
//        public IntPtr colorStreamHandle;
//        public IntPtr depthStreamHandle;

//        public KinectImage(int x, int y, int subSampleRatio)
//            : base(x, y, subSampleRatio)
//        {
//            whichImages = imagesToUpdate.allImages;

//            // don't use these anymore but needed for the polling functions
//            usersHistogramMap = new float[5000];
//            usersDepthMap = new short[noOfPixels];

//            // enable these if we want to do the people tracking shit
//            // if you just want to use the kinect as a colour webcam, comment these out
//            this.enableDepth();
//            this.enableUserMask();
//        }

//        // done as part of depth map
//        public override void refreshUserImage(){}

//        public override void refreshColorImage()
//        {
//            if (colorStreamHandle != IntPtr.Zero)
//                KinectWrapper.PollColor(colorStreamHandle, ref c32ColorImage, ref cvColorImage, true);
//        }

//        public override void refreshDepthImage()
//        {
//            if (depthStreamHandle != IntPtr.Zero)
//                KinectWrapper.PollDepth(depthStreamHandle, KinectManager.Instance.NearMode, ref usersDepthMap, ref cvDepthMap, ref cvUserMap, true);
//        }

//        public override Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte> getCVColorImage()
//        {
//            return cvColorImage;
//        }

//        // retain compatibility with normal webcam, assume we are using a mask based on depth/user data
//        public override void applyMaskToImage(Color32[] c32ColorDisplay, Emgu.CV.Image<Emgu.CV.Structure.Gray, short> mask)
//        {
//            applyMaskToImage(c32ColorDisplay, mask, true);
//        }


//        public override Emgu.CV.Image<Emgu.CV.Structure.Gray, short> getCVUserMap()
//        {
//            if (hasUserMask)
//            {
//                cvUserMap.CopyTo(cvUserMapCopy);
//                return cvUserMapCopy;
//            }
//            else
//                throw new Exception("User map not enabled!");
//        }


//        public void applyMaskToImage(Color32[] c32ColorDisplay, Emgu.CV.Image<Emgu.CV.Structure.Gray, short> mask, bool misalignedDepthMask)
//        {
//            // Flip the texture as we convert label map to color array
//            int flipIndex, i;
//            int x, y, cx, cy, hr, colorIndex;

//            // if this is a mask we are using based on colour image data
//            if (!misalignedDepthMask)
//            {
//                // Create the actual users texture based on label map and depth histogram
//                for (i = 0; i < noOfPixels; i++)
//                {
//                    flipIndex = noOfPixels - i - 1;
//                    x = i % 640;
//                    y = i / 640;

//                    c32ColorDisplay[flipIndex].a = (byte)mask.Data[y, x, 0];
//                }
//            }

            
//            // otherwise if its based on the depth or user map data, we need to find the true colour for 
//            // each pixel, as the cameras don't share the same view of the scene..
//            else
//            {
//                KinectWrapper.NuiImageViewArea pcViewArea = new KinectWrapper.NuiImageViewArea
//                {
//                    eDigitalZoom = 0,
//                    lCenterX = 0,
//                    lCenterY = 0
//                };

//                for (i = 0; i < noOfPixels; i++)
//                {
//                    flipIndex = noOfPixels - i - 1;
//                    x = i % KinectWrapper.Constants.ImageWidth;
//                    y = i / KinectWrapper.Constants.ImageWidth;

//                    if (mask.Data[y, x, 0] == 0)
//                    {
//                        //c32ColorDisplay[flipIndex].a = (byte) mask.Data[y,x,0];//(byte) (mask.Data[y,x,0] == 0 ? 0 : 255);
//                    }
//                    else
//                    {
//                        hr = KinectWrapper.NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(
//                            KinectWrapper.Constants.ImageResolution,
//                            KinectWrapper.Constants.ImageResolution,
//                            ref pcViewArea,
//                            x, y, usersDepthMap[i],
//                            out cx, out cy);
//                        //UnityEngine.Debug.LogError("got colour " + hr);
//                        if (hr == 0)
//                        {
//                            colorIndex = cx + cy * KinectWrapper.Constants.ImageWidth;
//                            colorIndex = noOfPixels - colorIndex - 1;
//                            if (colorIndex >= 0 && colorIndex < noOfPixels)
//                            {
//                                // doing this on the same array is probably
//                                // not a good idea, fix this later ;)
//                                c32ColorDisplay[flipIndex] = c32ColorImage[colorIndex];
//                            }
//                        }
//                        else
//                        {
//                            c32ColorDisplay[flipIndex] = c32ColorImage[flipIndex];
//                        }
//                    }

//                    c32ColorDisplay[flipIndex].a = (byte)(mask.Data[y, x, 0]);
//                }
//            }
//        }
//    }
//}
