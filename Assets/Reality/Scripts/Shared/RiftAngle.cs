using UnityEngine;
using System.Collections;
using System;

public class RiftAngle : MonoBehaviour {

    // for gaze mode 
    public static int keyboardAngle = 30;
    public static double cameraAngle;
    GameObject riftCamera;
    bool enabled = true;

    public int condition = 1;
    GameObject playerController, TestingWithoutRift;

    void Awake()
    {
        playerController = GameObject.Find("OVRPlayerController");
        TestingWithoutRift = GameObject.Find("TestingWithoutRift");

        // single monitor mode - typing
        if (condition == 1)
        {

            TestingWithoutRift.SetActive(true);
            playerController.SetActive(false);
            OVRPlayerController.disableGamepad = true;
            OVRPlayerController.disableKeyboard = true;
            MarkerTracker.disableConditionKeys = true;
            MarkerTracker.disableGamepadAndMiscCtrls = true;
            MarkerTracker.condition = MarkerTracker.conditions.baseline_typing;
        }
        // rift mode - typing
        else if (condition == 2)
        {
            playerController.SetActive(true);
            TestingWithoutRift.SetActive(false);
            OVRPlayerController.disableGamepad = true;
            OVRPlayerController.disableKeyboard = true;
            MarkerTracker.disableConditionKeys = true;
            MarkerTracker.disableGamepadAndMiscCtrls = true;
            MarkerTracker.condition = MarkerTracker.conditions.full_blending_inferred;
        }

        // rift mode - presence
        else if (condition == 3)
        {
            TestingWithoutRift.SetActive(false);
            playerController.SetActive(true);
            GameObject.Find("Typing-task").SetActive(false);
            OVRPlayerController.disableGamepad = false;
            OVRPlayerController.disableKeyboard = true;
            MarkerTracker.disableConditionKeys = false;
            MarkerTracker.disableGamepadAndMiscCtrls = false;
            MarkerTracker.condition = MarkerTracker.conditions.full_blending_user;

        }

        // chi video mode
        else if (condition == 4)
        {
            TestingWithoutRift.SetActive(true);
            playerController.SetActive(false);
            GameObject.Find("Typing-task").SetActive(false);
            OVRPlayerController.disableGamepad = false;
            OVRPlayerController.disableKeyboard = true;
            MarkerTracker.disableConditionKeys = false;
            MarkerTracker.disableGamepadAndMiscCtrls = false;
            MarkerTracker.condition = MarkerTracker.conditions.full_blending_inferred;

        }
    }

    void getRiftAngle()
    {
        if (enabled)
        {
            try
            {
                if (riftCamera == null)
                {
                    riftCamera = GameObject.Find("CameraLeft");
                    if (riftCamera == null)
                        Debug.Log("Cant find rift camera?");
                }
                cameraAngle = riftCamera.transform.eulerAngles.x;
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't get rift camera angle: " + ex.ToString());
                enabled = false;
            }
        }
    }


	// Use this for initialization
	void Start () {
        OVRDevice.ResetOrientation();
        getRiftAngle();
	}
	
	// Update is called once per frame
	void Update () {
        getRiftAngle();
	}
}
