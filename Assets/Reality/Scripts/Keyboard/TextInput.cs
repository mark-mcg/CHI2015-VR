using UnityEngine;
using System.Collections;
using Assets.Scripts.Keyboard;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Assets.Scripts.Keyboard
{
    class TextInput : MonoBehaviour
    {
        /*
         * Hacky class to support receiving textinput and sending it to one object
         */

        public interface TextReceiver
        {
            void ReceiveText(string transcribedText, string inputStream, bool execute);
        }

        static List<TextReceiver> textReceivers = new List<TextReceiver>();
        bool warnOnce = true;

        // Update is called once per frame
        int lenLastString = 0;
        bool execute = false;
        string transcribedText = "";
        string inputStream = "";
        public static bool canType = true;

        void Start()
        {
        }

        void Update()
        {
            try
            {
                execute = false;

                // check if the message has been flagged to be cleared
                if (clearMessage)
                {
                    transcribedText = "";
                    inputStream = "";
                    clearMessage = false;
                    Debug.LogError("Clearing message..");
                }

                lock (inputStream)
                {

                    if (canType && Input.inputString.Length > 0)
                    {
                        foreach (char c in Input.inputString)
                        {
                            // note the keystroke, whatever it was
                            if (c != '\n' && c != '\r')
                                inputStream += c;

                            // then enter it into our transcribed text

                            // Backspace - Remove the last character
                            if (c == "\b"[0])
                            {
                                if (transcribedText.Length != 0)
                                    transcribedText = transcribedText.Substring(0, transcribedText.Length - 1);
                            }
                            // End of entry
                            else if (c == '\n' || c == '\r')
                            {// "\n" for Mac, "\r" for windows.
                                // new line / enter
                                execute = true;
                            }
                            // Normal text input - just append to the end
                            else
                            {
                                transcribedText += c;
                            }
                        }
                    }

                    // if the string has changed.

                    if (transcribedText.Count() != lenLastString || execute)
                    {
                        lenLastString = transcribedText.Count();

                        // send the message on to the receivers
                        lock (textReceivers)
                        {
                            foreach (TextReceiver ts in textReceivers)
                            {
                                if (ts != null)
                                    ts.ReceiveText(transcribedText, inputStream, execute);
                                else
                                    Debug.LogError("TextReceiver is null for some reason?");
                            }
                        }
                    }

                    if (warnOnce && textReceivers.Count == 0)
                    {
                        UnityEngine.Debug.LogWarning("No textreceiver attached to TextInput object, you won't receive text entry!");
                        warnOnce = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("TextInput" + ex.ToString());
            }
        }

        static bool clearMessage = false;

        public static void clearMessageOnNextUpdate()
        {
            clearMessage = true;
        }

        public static void addTextReceiver(TextReceiver receiver)
        {
            textReceivers.Add(receiver);
            UnityEngine.Debug.Log("Added text receiver");
        }
    }
}
