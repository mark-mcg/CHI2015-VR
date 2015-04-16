using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.Scripts.Cameras;
using System.Threading;
using Assets.Scripts.Keyboard;
using System.Text.RegularExpressions;
using System.Timers;
using Assets.Scripts.Shared;

namespace Assets.Reality.Scripts.Keyboard
{
    class TextDisplays : MonoBehaviour, TextInput.TextReceiver
    {
        static TextDisplay left, right;
        static NetworkTextDisplay network;
        public static PhraseManager phrases;
        int[] args = new int[3];
        static System.Timers.Timer startTimer; // From System.Timers
        int startTime = 4000;
        int foundCounter = 0;
        public static int currentParticipant;
        public static bool active = false;

        GameObject OVRPlayerController, TestingWithoutRift;

        void Start(){
            active = true;
            TextInput.addTextReceiver(this);
            left = GameObject.Find("LeftRenderTextureCamera").GetComponent<TextDisplay>();
            right = GameObject.Find("RightRenderTextureCamera").GetComponent<TextDisplay>();
            network = GameObject.Find("Typing-task").GetComponent<NetworkTextDisplay>();

            phrases = new PhraseManager();
            setDisplayIsInstruction(true);

            MarkerTracker.alphaChangePerUpdate = 35;

            //OVRDevice.HMD.RecenterPose();

        }

        public void ReceiveText(string transcribedText, string inputStream, bool execute)
        {
            //KeyboardView.kb.secondLastKeyPressTime = KeyboardView.kb.lastKeyPressTime;
            //KeyboardView.kb.lastKeyPressTime = Environment.TickCount;

            //if (inputStream.Length > 0)
            //    KeyboardView.kb.lastKeyPressed = inputStream.Last();

            if (execute)
            {
                // if there was an enter key press, and the condition parser doesn't think it's a condition message
                // then it's the end of a phrase being entered
                if (!ConditionParser(transcribedText))
                {
                    lock (inputStream)
                    {
                        if (Application.loadedLevelName.Equals("VRDemo_Tuscany"))
                        {
                            TextInput.clearMessageOnNextUpdate();
                        }
                        else
                        {
                            phrases.finishPhrase(transcribedText, inputStream);
                            TextInput.clearMessageOnNextUpdate();
                        }
                    }
                }
            }
            else
            {
                // if its the first key press in a new phrase..
                phrases.noteFirstChar(transcribedText);
            }
        }

        bool OVRCamera = true;

        public bool ConditionParser(string condition)
        {
            if (condition.Contains("xquit"))
            {
                Application.Quit();
            }

            if (condition.Contains("xreset"))
            {
                OVRDevice.ResetOrientation();
                //OVRDevice.HMD.RecenterPose();
                TextInput.clearMessageOnNextUpdate();
                return true;
            }

            if (condition.Contains("xkeyboard"))
            {
                // if we've processed this message, clear it
                TextInput.clearMessageOnNextUpdate();

                return true;
            }
            else if (condition.Contains("xparticipant"))
            {
                string[] numbers = Regex.Split(condition, @"\D+");

                foreach (string value in numbers)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        currentParticipant = int.Parse(value);
                        break;
                    }
                }
                Debug.Log("Participant set to " + currentParticipant);
                TextInput.clearMessageOnNextUpdate();
                return true;
            }
            else if (condition.Contains("xcondition "))
            {
                foundCounter = 0;
                string[] numbers = Regex.Split(condition, @"\D+");

                //currentCondition = int.Parse(numbers[0]);

                foreach (string value in numbers)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        int i = int.Parse(value);
                        args[foundCounter] = i;
                        foundCounter++;
                    }
                }

                //currentCondition = args[0];
                Debug.Log("Condition set to: " + args[0]);
                MarkerTracker.setCondition(args[0]);

                // if we've processed this message, clear it
                TextInput.clearMessageOnNextUpdate();
                //UnityEngine.Debug.Log("Condition: " + currentCondition + " set");

                return true;
            }
            else if (condition.Contains("xstart"))
            {
                //UnityEngine.Debug.Log("starting condition " + currentCondition + " in " + startTime + "ms");
                TextInput.canType = false;
                sendTextToReadDisplay("Type the following messages out (hit enter to finish). Task starts in 5 seconds.");

                startTimer = new System.Timers.Timer(startTime); // Set up the timer for 3 seconds
                startTimer.Elapsed += new ElapsedEventHandler(this.startCondition);
                startTimer.Start();

                // if we've processed this message, clear it
                TextInput.clearMessageOnNextUpdate();

                return true;
            }

            return false;
        }


        public void startCondition(object sender, ElapsedEventArgs e)
        {
            Debug.Log("startCondition called");
            startTimer.Stop();
            //phrases.reset();
            phrases.noteNewBlock();
            phrases.nextPhrase();
        }

        public static void sendTextToReadDisplay(String message)
        {
            right.setInstructionOrPhrase(message);
            //network.ReceiveText(message, "", false);
        }

        public static void setDisplayIsInstruction(bool isInstruction)
        {
            right.setIsInstruction(isInstruction);
            //network.setIsInstruction(isInstruction);
        }

        void OnDestroy()
        {
            LogWriter.TextEntryLog.Close();
            LogWriter.HeadAngleLog.Close();
        }

    }
}
