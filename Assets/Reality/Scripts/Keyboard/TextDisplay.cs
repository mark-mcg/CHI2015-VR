using UnityEngine;
using System.Collections;
using Assets.Scripts.Keyboard;

public class TextDisplay : MonoBehaviour, TextInput.TextReceiver{

    // set these in the editor!
    public RenderTexture renderTarget;
    public bool receivesTextInput = true;

    EmailLayout gui;

	// Use this for initialization
	void Start () {
        if (renderTarget == null)
        {
            UnityEngine.Debug.LogError("Set the render texture in the editor or this won't work!");
        } else {
            UnityEngine.Debug.LogError("Creating email layout");
            if (receivesTextInput)
            {
                gui = new EmailLayout(renderTarget, "You can type on this display");
                TextInput.addTextReceiver(this);
            }
            else
            {
                gui = new EmailLayout(renderTarget, "This is your virtual display, text will appear here.");
                TextInput.addTextReceiver(this);
            }
        }
	}

    public void setInstructionOrPhrase(string message)
    {
        gui.phraseOrInstructionMessage = message;
    }

    public void ReceiveText(string transcribedText, string inputStream, bool execute)
    {
        gui.userText = transcribedText;
        //UnityEngine.Debug.Log("GUI message is " + gui.message);
    }

    public void setIsInstruction(bool isInstruction)
    {
        if (gui != null)
        {
            gui.isInstruction = isInstruction;
        }
    }

    int guiUpdate = 10;

    void OnGUI()
    {

        if (gui != null) {

            if (guiUpdate > 0)
            {
                gui.OnGUI();
                guiUpdate = 0;
            }
            guiUpdate++;
        }
    }
}
