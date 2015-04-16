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
 
public class TestProcessor : AbstractImageProcessor
{
    public TestProcessor(int x, int y, bool debug = false, int contourArea = 15, int resize = 1)
        : base(x, y)
    {

	}

	public override void processImage(Emgu.CV.Image<Rgba, byte> cvColorImage, UnityEngine.Color[] alphaMask)
    {
		for (int cvx = 0; cvx < AbstractVideoDevice.cvWidth; cvx++)
		{
            for (int cvy = 0; cvy < AbstractVideoDevice.cvHeight; cvy++)
			{
				cvMask.Data[cvy, cvx, 0] = 255;
			}
		}

        imageCopier.parallelCopy(cvMask, alphaMask);  
    }

	public override void handleKeyPresses(){

	}

	public override void OnGUI(){
		
	}
}


