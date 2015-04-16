using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public class ParallelImageCopier {
	
	// No of processing threads to use
	int noOfThreads = 6;
	
	// Our processing threads
	Thread[] threads;
	AutoResetEvent[] threadEvents;  // this is for signalling the threads to do work
	AutoResetEvent[] waitOnThreads; // this is for signalling the management thread 
									// that the processing threads are finished working
	string[] locks;
	
	// Image being processed
	Emgu.CV.Image<Gray, byte> from;
	Color[] to;
	int fromWidth;
	int fromHeight;
	int recalculateSegments = 0;

	public ParallelImageCopier(){
		
		// setup our processing threads
		threads = new Thread[noOfThreads];
		locks = new string[noOfThreads];
		threadEvents = new AutoResetEvent[noOfThreads];
		waitOnThreads = new AutoResetEvent[noOfThreads];
		
		for (int  i = 0; i < noOfThreads; i++){
			threads[i] = new Thread(parallelTask);
			threads[i].Priority = System.Threading.ThreadPriority.Highest;
			locks[i] = "lol";
			threadEvents[i] = new AutoResetEvent(false);
			waitOnThreads[i] = new AutoResetEvent(false);
			System.Object val = i;
			threads[i].Start(val);
		}
	}
	
	// Subsampled given image using noOfThreads threads to process in parallel. Much faster!
	public void parallelCopy(Emgu.CV.Image<Gray, byte> from, Color[] to)
	{	
		// set our image to be processed
		this.to = to;
		this.from = from;
		this.fromWidth = from.Width;
		this.fromHeight = from.Height;

		if (this.fromWidth != from.Width || this.fromHeight != from.Height)
			recalculateSegments = 0;

		// wake our threads up so they start processing their individual chunks
		foreach(AutoResetEvent threadWaitLock in threadEvents){
			threadWaitLock.Set();
		}
		
		// wait for our processing threads to finish
		WaitHandle.WaitAll(waitOnThreads);
	}

	private void parallelTask(System.Object val){
		int whatThread = (int)val;
		int startHeight =0 , endHeight = 0, segmentSize = 0;

		while (locks[whatThread] != null){
			threadEvents[whatThread].WaitOne();

			// if we need to, calculate what area of image we are processing
			if (recalculateSegments < noOfThreads){
				segmentSize = Convert.ToInt32(  fromHeight /  (float)noOfThreads  );

				startHeight = whatThread * segmentSize;
				endHeight = (whatThread+1) * segmentSize;
				
				if (fromHeight - endHeight < segmentSize)
					endHeight = fromHeight;

				recalculateSegments++;
			}

			//Debug.Log ("subCopySection() - thread " + whatThread + ", " + segmentSize + ", " + startHeight + ", " + endHeight);

			// process section
			subCopySection (startHeight, endHeight);

			waitOnThreads[whatThread].Set ();
		}
	}
	
	private void subCopySection(int startHeight, int endHeight)
	{
		int index;
		
		for (int toX = 0; toX < fromWidth; toX++)
		{
			for (int toY = startHeight; toY < endHeight; toY++)
			{
				index = toX + (toY * fromWidth);
				to[index].a = from.Data[toY, toX, 0] / 255.0f;
			}
		}
	}
	
	public void OnApplicationQuit(){
		for (int i = 0; i < noOfThreads; i++){
			locks[i] = null;
			threads[i].Abort();
		}
	}

	public void copyCVMasktoColorSingleThreaded(Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> mask, UnityEngine.Color[] colorMask)
	{
		int index;
		for (int x = 0; x < mask.Width; x++)
		{
			for (int y = 0; y < mask.Height; y++)
			{
				index = (x + (y * AbstractVideoDevice.cvHeight));
				colorMask[index].a = mask.Data[y, x, 0] / 255.0f;
			}
		}
	}


}
