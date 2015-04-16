using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


/*
 * Subsample images in parallel 
 * 
 * TODO: Investigate using paralell.for loop, as it might be faster
 * http://msdn.microsoft.com/en-us/library/dd460713(v=vs.110).aspx
 * although unity probably doesn't support it out the box (or at all?)
 * 
 * n.b. tried threadpool approach, it was crap, pool got swamped too easily
 * 
 */ 
public class ParallelSubSampler {

	// No of processing threads to use
	int noOfThreads = 6;

	// Our processing threads
	Thread[] subSamplingThreads;
	AutoResetEvent[] threadEvents; // this is for signalling the threads to do work
	AutoResetEvent[] waitOnThreads; // this is for signalling the management thread 
									// that the processing threads are finished working
	string[] locks;

	// Image being processed
	Emgu.CV.Image<Rgba, byte> to;
	Color32[] from;
	int fromWidth;
	int fromHeight;
	int recalculateSegments = 0;

	public ParallelSubSampler(){

		// setup our processing threads
		subSamplingThreads = new Thread[noOfThreads];
		locks = new string[noOfThreads];
		threadEvents = new AutoResetEvent[noOfThreads];
		waitOnThreads = new AutoResetEvent[noOfThreads];
		
		for (int  i = 0; i < noOfThreads; i++){
			subSamplingThreads[i] = new Thread(parallelTask);
			subSamplingThreads[i].Priority = System.Threading.ThreadPriority.Highest;
			locks[i] = "lol";
			threadEvents[i] = new AutoResetEvent(false);
			waitOnThreads[i] = new AutoResetEvent(false);
			System.Object val = i;
			subSamplingThreads[i].Start(val);
		}
	}

	// Subsampled given image using noOfThreads threads to process in parallel. Much faster!
	public void parallelSubSampleC32toCV(Emgu.CV.Image<Rgba, byte> to, Color32[] from, int fromWidth, int fromHeight)
	{	
		// set our image to be processed
		this.to = to;
		this.from = from;
		this.fromWidth = fromWidth;
		this.fromHeight = fromHeight;

		if (this.fromWidth != fromWidth || this.fromHeight != fromHeight)
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
				segmentSize = Convert.ToInt32(  to.Height /  (float)noOfThreads  );

				startHeight = whatThread * segmentSize;
				endHeight = (whatThread+1) * segmentSize;
				
				if (to.Height - endHeight < segmentSize)
					endHeight = to.Height;
				recalculateSegments++;
			}
			
			//Debug.Log ("subSampleSection() - thread " + whatThread + ", " + segmentSize + ", " + startHeight + ", " + endHeight);
			
			// process section
			subSampleSection (startHeight, endHeight);
			
			waitOnThreads[whatThread].Set ();
		}
	}
	
	private void subSampleSection(int startHeight, int endHeight)
	{
		int index;
		double xRatio = fromWidth / (double)to.Width;
		double yRatio = fromHeight / (double)to.Height;
		
		for (int toX = 0; toX < to.Width; toX++)
		{
			for (int toY = startHeight; toY < endHeight; toY++)
			{
				index = (Convert.ToInt32(toX * xRatio)) + (Convert.ToInt32(toY * yRatio) * fromWidth);
				to.Data[toY, toX, 0] = from[index].b;
				to.Data[toY, toX, 1] = from[index].g;
				to.Data[toY, toX, 2] = from[index].r;
			}
		}
	}

	public void OnApplicationQuit(){
		for (int i = 0; i < noOfThreads; i++){
			locks[i] = null;
			subSamplingThreads[i].Abort();
		}
	}


	// Old way: do the subsampling on the calling thread sequentially. Left for benchmarking..
	public void subSampleC32toCVSingleThreaded(Emgu.CV.Image<Rgba, byte> to, Color32[] from, int fromWidth, int fromHeight)
	{
		
		int index;
		double xRatio = fromWidth / (double)to.Width;
		double yRatio = fromHeight / (double)to.Height;
		
		for (int toX = 0; toX < to.Width; toX++)
		{
			for (int toY = 0; toY < to.Height; toY++)
			{
				index = (Convert.ToInt32(toX * xRatio)) + (Convert.ToInt32(toY * yRatio) * fromWidth);
				to.Data[toY, toX, 0] = from[index].b;
				to.Data[toY, toX, 1] = from[index].g;
				to.Data[toY, toX, 2] = from[index].r;
			}
		}
	}

}
