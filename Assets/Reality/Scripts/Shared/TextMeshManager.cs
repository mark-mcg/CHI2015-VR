using UnityEngine;
using System.Collections;
using System.Threading;
using System;

public class TextMeshManager : MonoBehaviour {
	static TextMesh textMesh;
	//static GameObject textMeshBackground;
	static GameObject textMeshObject;

	// Use this for initialization
	void Start () {
		textMeshObject = GameObject.Find ("UserMessageTextMesh");
		//textMeshBackground = GameObject.Find ("TextMeshBackground");
		textMesh = textMeshObject.GetComponent<TextMesh>();
        textMeshObject.SetActive(false);
	}
	

	void Update () {
		if (updated){
            if (textMesh != null && textMeshObject != null)
            {
                textMesh.text = text;
                textMeshObject.SetActive(textEnabled);
                updated = false;
            }
		}


	}

	static string text = "no text";
	static bool textEnabled = false;
	static bool updated = true;
	static Timer updateTimer;

	public static void showTextForPeriod(string textToSet, long periodInSeconds){
        Debug.Log("Showing message: " + textToSet);
		text = textToSet;
		textEnabled = true;
		updated = true;


		if (updateTimer != null)
			updateTimer.Dispose();

		updateTimer = new Timer(disableText, null,
		                              TimeSpan.FromMilliseconds(periodInSeconds * 1000),   // Delay by 1ms
		                              TimeSpan.FromMilliseconds(-1)); // Never repeat
	}

	public static void disableText(System.Object state){
		Debug.Log ("Disabling text");
		textEnabled = false;
		updated = true;
	}
}
