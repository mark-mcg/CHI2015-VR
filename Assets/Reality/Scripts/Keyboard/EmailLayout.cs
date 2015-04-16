using UnityEngine;
using System.Collections;
using Assets.Scripts.Keyboard;

// http://angryant.com/2013/07/17/OnRenderTextureGUI/

public class EmailLayout  {

    public RenderTexture m_TargetTexture = null;
    RenderTexture m_PreviousActiveTexture = null;

	public string phraseOrInstructionMessage = "This box goes on a render texture! LOLOLOLOLOLOLOLOLOOLOOOOOOOL";
    public string userText = "";

	GUIStyle phraseStyle;
	GUIStyle instructionStyle;
    GUIStyle headingStyle;
    bool doOnce = true;
    public bool isInstruction = true;

    public EmailLayout(RenderTexture target, string message)
    {
        this.phraseOrInstructionMessage = message;
        this.m_TargetTexture = target;
    }



    public void OnGUI ()
	{
        if (doOnce)
        {
            // setup our layout crudely
            phraseStyle = new GUIStyle(GUI.skin.label);
            phraseStyle.fontSize = 200;
            phraseStyle.normal.textColor = Color.black;
            phraseStyle.normal.background = MakeTex(2, 2, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            phraseStyle.wordWrap = true;
            phraseStyle.alignment = TextAnchor.MiddleLeft;
            phraseStyle.padding = new RectOffset(50, 50, 12, 12);

            instructionStyle = new GUIStyle(phraseStyle);


            headingStyle = new GUIStyle(phraseStyle);
            headingStyle.alignment = TextAnchor.UpperCenter;
            headingStyle.normal.background = MakeTex(2, 2, new Color(0.5f, 1.0f, 0.5f, 0.5f));


            doOnce = false;
        }

        BeginRenderTextureGUI(m_TargetTexture);

        GUILayout.BeginArea(new Rect(0, 0, 2048, 2048));

        if (!isInstruction)
        {
            GUILayout.Label("Phrase to type:", headingStyle);
            GUILayout.TextArea(phraseOrInstructionMessage, phraseStyle, GUILayout.ExpandHeight(false));
            GUILayout.Label("Your text:", headingStyle);
            GUILayout.TextArea(userText, phraseStyle, GUILayout.ExpandHeight(false));

        }
        else
        {
            GUILayout.Label("Instruction", headingStyle);
            GUILayout.TextArea(phraseOrInstructionMessage, instructionStyle, GUILayout.ExpandHeight(false));
            GUILayout.TextArea(userText, phraseStyle, GUILayout.ExpandHeight(false));
        }

        GUILayout.EndArea();

        EndRenderTextureGUI();

	}


    int repaint = 4;
	protected void BeginRenderTextureGUI (RenderTexture targetTexture)
	{
		if (Event.current.type == EventType.Repaint)
		{
			m_PreviousActiveTexture = RenderTexture.active;
			if (targetTexture != null)
			{
				RenderTexture.active = targetTexture;
				GL.Clear (false, true, Color.white);
			}
		}
	}
	
	
	protected void EndRenderTextureGUI ()
	{
		if (Event.current.type == EventType.Repaint)
		{
			RenderTexture.active = m_PreviousActiveTexture;
		}
	}


	private Texture2D MakeTex( int width, int height, Color col )
		
	{
		
		Color[] pix = new Color[width * height];
		
		for( int i = 0; i < pix.Length; ++i )
			
		{
			
			pix[ i ] = col;
			
		}
		
		Texture2D result = new Texture2D( width, height );
		
		result.SetPixels( pix );
		
		result.Apply();
		
		return result;
		
	}
}
