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

public abstract class AbstractVideoDevice {

    protected GameObject renderPlain;
    protected MeshRenderer renderPlainRenderer;
    protected Texture2D alphaTexture;
    protected Renderer renderer;

    public bool hasDepth = false;
    public bool hasUserMask = false;
    public bool hasMask = true;
    public bool hasColorImage = true;

    protected int noOfPixels;

    public int x, y;
    protected int subx, suby;

    public static int cvHeight = 256; // keep as standard texture sizes!
    public static int cvWidth = 256;

	protected int frameID = 0;

    protected AutoResetEvent toBeNotified;

    protected DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> colourImagesBuffer;

    public AbstractVideoDevice(Renderer renderer)
    {
        // stuff related to rendering the video
        this.renderer = renderer;

        // this is the alpha texture for this video
        alphaTexture = new Texture2D(cvWidth, cvHeight, TextureFormat.Alpha8, false);

        // this is the plain we are going to render to
        renderPlain = GameObject.Find("RenderPlain");
        renderPlainRenderer = renderPlain.GetComponent<MeshRenderer>();
        renderer.material.SetTexture("_Alpha", alphaTexture);

        // Our buffer of colour images to be processed
        // we downsample to cvWidth / cvHeight
        colourImagesBuffer = DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>>.DisplayBufferFactoryColorCV(cvWidth, cvHeight);
    }

    public DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> getColourImagesBuffer()
    {
        return colourImagesBuffer;
    }

    public void notifyOnNewFrame(AutoResetEvent t)
    {
        toBeNotified = t;
    }

    public Texture2D getAlphaTexture()
    {
        return alphaTexture;
    }

    public void Update()
    {
        try
        {
            UpdateImageBuffers();
        }
        catch (Exception ex)
        {
            Debug.LogError("AbstractImage" + ex.ToString());
        }
    }
    
    /*
     * Tell our video device to update it's image data
     * If there's a new image, stick it in the CV images buffer
     * 
     * Return true if it got a new frame, otherwise false
     */
    public abstract bool UpdateImageBuffers();
    public abstract void OnApplicationQuit();

	public void setFrame(int frame){
		this.frameID = frame;
	}

    #region oldstuff

    //protected virtual void applyCVMaskToC32Image(Color32[] imageTo, Emgu.CV.Image<Emgu.CV.Structure.Gray, short> mask)
    //{
    //    int index;

    //    // we're assuming the imageTo is at our full resolution x y and not subx suby
    //    for (int x = 0; x < subx; x++)
    //    {
    //        for (int y = 0; y < suby; y++)
    //        {
    //            index = (x * subSampleRatio) + (y * subSampleRatio * this.x);        
    //            // we're assuming we have subsampled at half the resolution cos i can't be fucked doing more
    //            // so each pixel in the mask corresponds to 4 pixels in the colour image
    //            // index, index + 1, index + width, index + width + 1

    //            // loop through our subsampled block of pixels setting their alpha to current alpha of mask
    //            for (int indexX = 0; indexX < subSampleRatio; indexX++)
    //            {
    //                for (int indexY = 0; indexY < subSampleRatio; indexY++) {
    //                    imageTo[index + indexX + (indexY * this.x)].a = (byte)mask.Data[y, x, 0];
    //                }
    //            }

    //        }
    //    }

    //}
    #endregion

}
