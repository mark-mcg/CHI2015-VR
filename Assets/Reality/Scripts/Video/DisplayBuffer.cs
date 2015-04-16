using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Assets.Scripts.Processors
{

    public class DisplayBuffer<T>
    {
        public class display
        {
            private T theDisplay;

            public int age;
            public bool locked = false;
            public bool hasBeenDisplayed = false;
			public int id = 0; // for tracking frame through pipeline

            public display(T display, int age)
            {
                this.age = age;
                this.theDisplay = display;
            }


            public T getDisplay(){
                return theDisplay;
            }
        }

        public int buffersize = 4;
        display[] buffer;

        private DisplayBuffer(int size = 4)
        {
			buffersize = size;
            buffer = new display[buffersize];
        }

        private void addBuffer(T bufferObject, int index)
        {
            buffer[index] = new display(bufferObject, Environment.TickCount);
        }

        public display getOldestDisplay()
        {
			int oldestAge = Environment.TickCount + 100;
			int oldestDisplay = -1;
			bool allLocked = true;

			lock(buffer){
		        for (int i = 0; i < buffersize; i++)
		        {
					allLocked = allLocked & buffer[i].locked;

		            if (!buffer[i].locked)
		            {
		                if (buffer[i].age < oldestAge)
		                {
		                    oldestDisplay = i;
		                    oldestAge = buffer[i].age;
		                }
		            }
		        }

				if (oldestDisplay != -1){
		        	buffer[oldestDisplay].locked = true;
					//Debug.Log("Got oldest of " + oldestDisplay);
				} else {
					Debug.LogError ("DisplayBuffer getOldestDisplay: failed to get display, allLocked " + allLocked);
					oldestDisplay = 0;
				}
			}
	    
            return buffer[oldestDisplay];
        }

        public display getNewestDisplay()
        {
			int newestAge = -1;
			int newestDisplay = -1;
			bool allLocked = true;

			lock(buffer){
		        for (int i = 0; i < buffersize; i++)
		        {
					allLocked = allLocked & buffer[i].locked;
		            if (!buffer[i].locked)
		            {
		                if (buffer[i].age > newestAge)
		                {
		                    newestDisplay = i;
		                    newestAge = buffer[i].age;

		                }
		            }
		        }
						    
				if (newestDisplay != -1){
					buffer[newestDisplay].locked = true;
					//Debug.Log("Got newest of " + newestDisplay);
				} else {
					Debug.LogError ("DisplayBuffer getNewestDisplay: failed to get display, allLocked " + allLocked);
					newestDisplay = 0;
				}
			}
            return buffer[newestDisplay];
        }

        public void releaseLock(display d, bool updateAge)
        {
			//bool foundDisplay = false;
			//bool wasLocked = false;

            for (int i = 0; i < buffersize; i++)
            {
                if (buffer[i] == d)
                {
					//foundDisplay = true;
					//wasLocked = buffer[i].locked;
	                buffer[i].locked = false;
	                if (updateAge)
	                    buffer[i].age = Environment.TickCount;
                }
            }

			//if (foundDisplay)
			//	Debug.Log ("release lock: success " + wasLocked);
			//else
			//	Debug.Log ("release lock: FAILED " + wasLocked);

        }

        #region factory methods (lol not quite right, redo this later!)
        public static DisplayBuffer<Color[]> DisplayBufferFactoryColor(int noOfPixels)
        {
            DisplayBuffer<Color[]> db = new DisplayBuffer<Color[]>();

            for (int i = 0; i < db.buffersize; i++)
            {
                db.addBuffer(new Color[noOfPixels], i);
            }

            return db;
        }

        public static DisplayBuffer<Color32[]> DisplayBufferFactoryColor32(int noOfPixels)
        {
            DisplayBuffer<Color32[]> db = new DisplayBuffer<Color32[]>();

            for (int i = 0; i < db.buffersize; i++)
            {
                db.addBuffer(new Color32[noOfPixels], i);
            }

            return db;
        }

        public static DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> DisplayBufferFactoryColorCV(int x, int y)
        {
            DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> db = new DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>>();

            for (int i = 0; i < db.buffersize; i++)
            {
                db.addBuffer(new Emgu.CV.Image<Rgba, byte>(x, y), i);
            }

            return db;
        }

        public static DisplayBuffer<Emgu.CV.Image<Gray, short>> DisplayBufferFactoryGrayCV(int x, int y)
        {
            DisplayBuffer<Emgu.CV.Image<Gray, short>> db = new DisplayBuffer<Emgu.CV.Image<Gray, short>>();

            for (int i = 0; i < db.buffersize; i++)
            {
                db.addBuffer(new Emgu.CV.Image<Gray, short>(x, y), i);
            }

            return db;
        }




		public static DisplayBuffer<Color[]> DisplayBufferFactoryColor(int noOfPixels, int size)
		{
			DisplayBuffer<Color[]> db = new DisplayBuffer<Color[]>(size);
			
			for (int i = 0; i < db.buffersize; i++)
			{
				db.addBuffer(new Color[noOfPixels], i);
			}
			
			return db;
		}
		
		public static DisplayBuffer<Color32[]> DisplayBufferFactoryColor32(int noOfPixels, int size)
		{
			DisplayBuffer<Color32[]> db = new DisplayBuffer<Color32[]>(size);
			
			for (int i = 0; i < db.buffersize; i++)
			{
				db.addBuffer(new Color32[noOfPixels], i);
			}
			
			return db;
		}
		
		public static DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> DisplayBufferFactoryColorCV(int x, int y, int size)
		{
			DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>> db = new DisplayBuffer<Emgu.CV.Image<Emgu.CV.Structure.Rgba, byte>>(size);
			
			for (int i = 0; i < db.buffersize; i++)
			{
				db.addBuffer(new Emgu.CV.Image<Rgba, byte>(x, y), i);
			}
			
			return db;
		}
		
		public static DisplayBuffer<Emgu.CV.Image<Gray, short>> DisplayBufferFactoryGrayCV(int x, int y, int size)
		{
			DisplayBuffer<Emgu.CV.Image<Gray, short>> db = new DisplayBuffer<Emgu.CV.Image<Gray, short>>(size);
			
			for (int i = 0; i < db.buffersize; i++)
			{
				db.addBuffer(new Emgu.CV.Image<Gray, short>(x, y), i);
			}
			
			return db;
		}
        #endregion


    }
}
