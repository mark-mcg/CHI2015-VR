//using UnityEngine;
//using System.Collections;
//
//public class TransformCameraPlane : MonoBehaviour {
//    GameObject plain;
//    GameObject OVRPlayerController;
//    GameObject KinectUserPosition;
//
//	// Use this for initialization
//	void Start () {
//        plain = GameObject.Find("RenderPlain");
//        OVRPlayerController = GameObject.Find("OVRCameraController");
//        KinectUserPosition = GameObject.Find("U");
//	}
//	
//	int blah = 0;
//	// Update is called once per frame
//    Vector3 newPosition;
//
//	void Update () {
//		if (plain != null) {
//
//            if (VideoDeviceProcessor.ComputeUserAndDepthMap)
//            {
//                // user tracking mode!
//
//                // we're going to orient the plane so that it is infront of the oculus rift view
//                newPosition = OVRPlayerController.transform.position;
//
//                // but offset slightly to make it look right
//                newPosition.z -= 2.3f;
//                //newPosition.y += 2;
//
//                // then we're going to add the movement of the tracked Kinect user
//                newPosition.x -= KinectUserPosition.transform.localPosition.x;
//                newPosition.y += KinectUserPosition.transform.localPosition.y + 0.1f;
//                newPosition.z += KinectUserPosition.transform.localPosition.z;
//
//                // then finally make sure its at the correct rotation (shit be upside down yo)
//                plain.transform.position = newPosition;
//                plain.transform.localScale = new Vector3(1.333f * 0.75f / 3, 1.333f * 0.75f / 3, 0.75f / 3);
//                plain.transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);   
//
//                // then make sure it's looking at the users camera
//                //plain.transform.rotation = Quaternion.LookRotation(OVRPlayerController.transform.position) * Quaternion.Euler(0, 180, 0);
//            }
//            else
//            {
//                // keyboard mode!
//
//            }
//		} 
//	}
//}
