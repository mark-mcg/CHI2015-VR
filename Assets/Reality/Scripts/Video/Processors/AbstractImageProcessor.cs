using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Assets.Scripts.Processors;
using UnityEngine;

namespace Assets.Scripts.Processors
{
    public abstract class AbstractImageProcessor
    { 

        protected int cWidth, cHeight;
        protected Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> cvMask;
		protected ParallelImageCopier imageCopier;

        public AbstractImageProcessor(int x, int y)
        {
            this.cWidth = x;
            this.cHeight = y;
            cvMask = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(x, y);
			imageCopier = new ParallelImageCopier();
        }

        // gets a cvColorImage of dimensions AbstractVideoDevice.cvWidth / cvHeight, and 
        // and an alphaMask - this is the mask that is going to be uploaded to the GPU, so either
        // draw on it directly, or draw on the cvColorImage then use the copyCVMaskToColor() method
        public abstract void processImage(Emgu.CV.Image<Rgba, byte> cvColorImage, Color[] alphaMask);

        // Incase your processor wants to react to inputs for changing parameters
        public abstract void handleKeyPresses();

		public abstract void OnGUI();

		public void OnApplicationQuit(){
			imageCopier.OnApplicationQuit();
		}
		
    }
}
