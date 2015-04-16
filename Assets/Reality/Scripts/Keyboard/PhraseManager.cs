using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Assets.Scripts.Keyboard;
using System.Timers;
using Assets.Scripts.Cameras;
using System.IO;
using Assets.Scripts.Shared;
using Assets.Reality.Scripts.Keyboard;

namespace Assets.Scripts.Cameras
{
    public class PhraseManager
    {
        Phrases phrases;
        static Phrase currentPhrase;
        Timer nextTimer;
        static Timer angleLogTimer;
        int phrasesShown = 0;
        static int phrasesPerBlock = 14 + 1; // 1 training phrase at start which is ignored
        bool newBlock = true;
        bool doingBlock = false;

        public PhraseManager(){
            phrases = new Phrases();
        }

        public void noteNewBlock()
        {
            newBlock = true;
            doingBlock = true;
        }

        public bool isDoingBlock()
        {
            return doingBlock;
        }

        public void nextPhrase()
        {
            TextDisplays.setDisplayIsInstruction(true);
            TextDisplays.sendTextToReadDisplay("Please look straight ahead, and put your hands by your side until the next phrase appears.");
            TextInput.canType = false;

            if (nextTimer == null)
            {
                nextTimer = new Timer(4000); // Set up the timer for 4 seconds
                nextTimer.Elapsed += new ElapsedEventHandler(this.showNextMessageDelayed);
            }
            nextTimer.Start();
        }

        void showNextMessageDelayed(object sender, ElapsedEventArgs e)
        {
            Debug.Log("showNextMessageDelayed called " + Environment.TickCount);
            nextTimer.Stop();


            try
            {
                //OVRDevice.ResetOrientation(0);
                currentPhrase = phrases.getNextRandomPhrase();
                currentPhrase.firstChar = true;
                TextInput.canType = true;
                TextDisplays.setDisplayIsInstruction(false);

                currentPhrase.phraseShownAtMS = Environment.TickCount;
                TextDisplays.sendTextToReadDisplay(currentPhrase.phrase);
                startAngleLogger();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        // when a phrase has been entered, call this
        public void finishPhrase(string transcribedText, string inputStream)
        {
            if (currentPhrase != null)
            {
                stopAngleLogger();
                phrasesShown++;
                textMetrics(transcribedText, inputStream, currentPhrase.phrase);
                currentPhrase = null;

                if (phrasesShown == phrasesPerBlock)
                {
                    TextDisplays.setDisplayIsInstruction(true);
                    TextDisplays.sendTextToReadDisplay("Finished block of " + phrasesShown + " phrases");
                    phrasesShown = 0;
                    doingBlock = false;
                }
                else
                {
                    nextPhrase();
                }
            }
        }

        public void noteFirstChar(string transcribedText)
        {
            if (currentPhrase != null)
            {
                if (currentPhrase.firstChar && transcribedText.Count() != 0)
                {
                    currentPhrase.firstCharAtMS = Environment.TickCount;
                    currentPhrase.firstChar = false;
                }
            }
        }

        public static void startAngleLogger()
        {
            if (angleLogTimer == null)
            {
                angleLogTimer = new Timer(70);
                LogWriter.HeadAngleLog.WriteToLog(string.Format("{0}\t{1}\t{2}\t{3}",
                    "Condition", "Participant", "ID", "Angle"));
                angleLogTimer.Elapsed += new ElapsedEventHandler(logCameraAngle);
            }
            angleLogTimer.Start();

        }

        public static void stopAngleLogger()
        {
            angleLogTimer.Stop();
        }

        public static void logCameraAngle(object sender, ElapsedEventArgs e)
        {
            LogWriter.HeadAngleLog.WriteToLog(string.Format("{0}\t{1}\t{2}\t{3}",
                ""+MarkerTracker.condition, TextDisplays.currentParticipant, currentPhrase.id, 0));  
        }



        void textMetrics(string transcribedText, string inputStream, string presentedText)
        {
            if (newBlock)
            {
                // we aren't using the first phrase, so don't bother with this one
                newBlock = false;
                return;
            }
            try
            {
                // Entry speed (WPM): length of text / entry time (in seconds) * 60 / 5 (accepted word length)
                // include enter key in keystroke count!
                double durationTyping = (Environment.TickCount - currentPhrase.firstCharAtMS);
                double durationToFirstKey = (currentPhrase.firstCharAtMS - currentPhrase.phraseShownAtMS);
                double WPM = (((transcribedText.Length + 1) / (durationTyping / 1000)) * 60) / 5;
                int firstKeyCorrect = 0;

                if (inputStream[0] == presentedText[0])
                    firstKeyCorrect = 1;

                double MSD = LevenshteinDistance.getMSDErrorRate(presentedText, transcribedText);

                double INF = LevenshteinDistance.MSDLD;
                double F = inputStream.Count(f => f == '\b');
                double IF = inputStream.Length - transcribedText.Length - F;
                double C = LevenshteinDistance.C;

                //UnityEngine.Debug.Log("PRESENTED:" + presentedText + ": length " + presentedText.Length);
                //UnityEngine.Debug.Log("TRANSCRIB:" + transcribedText + ": length " + transcribedText.Length);
                //UnityEngine.Debug.Log("INPUTSTRE:" + inputStream + ": length " + inputStream.Length);
                UnityEngine.Debug.LogError("WPM " + WPM + " durationToFirstKey " + durationToFirstKey + " durationTyping " + durationTyping);
                UnityEngine.Debug.Log("INF " + INF + " C " + C + " F " + F + " IF " + IF);
                /*
                 * Corrected metrics..
                 * 
                 *  Use MSD to find the INF keystrokes
                 *  All keystrokes - INF = C
                 *  F - editing functions (backspace)
                 *  IF - any chars in the input stream but not the transcribed text that arent editing keys
                 * 
                 */

                // MSD in keystroke taxonomy
                double MSDER = (INF / (C + INF)) * 100;
                double totalErrorRate = ((INF + IF) / (C + INF + IF)) * 100;
                double notCorrectedErrorRate = (INF / (C + INF + IF)) * 100;
                double correctedErrorRate = (IF / (C + INF + IF)) * 100;
                UnityEngine.Debug.LogError("MSDER " + MSDER + " totalErrorRate " + totalErrorRate + " notCorrectedErrorRate " + notCorrectedErrorRate + " correctedErrorRate " + correctedErrorRate);

                String message = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                    "" + MarkerTracker.condition, TextDisplays.currentParticipant, currentPhrase.id,
                    transcribedText,
                    inputStream,
                    presentedText,
                    WPM, durationToFirstKey, durationTyping, INF, C, F, IF, MSDER, totalErrorRate, notCorrectedErrorRate, correctedErrorRate, firstKeyCorrect);

                LogWriter.TextEntryLog.WriteToLog(message);
            }
            catch (Exception ex) {
                Debug.LogError("Metrics broke, why?" + ex.ToString());
                LogWriter.TextEntryLog.WriteToLog("Metrics broke, why?" + ex.ToString());
            }
        }


        class Phrases{
            Dictionary<String, Phrase> phrases;
            List<String> validChoices;
            System.Random r = new System.Random();

            public Phrases(){

                phrases = new Dictionary<String, Phrase>();
                validChoices = new List<String>();

                /*for (int i = 1; i <= 5; i++)
                {
                    UnityEngine.Debug.Log("Opening /Scripts/phrase-sets/mem" + i + ".txt");
                    StreamReader sr = new StreamReader("C:/phrase-sets/mem" + i + ".txt");

                    String line = sr.ReadLine();
                    string[] split;
                    while (line != null)
                    {
                        split = line.Split('\t');
                        phrases.Add(split[0], new Phrase(split[0], split[1]));
                        validChoices.Add(split[0]);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }*/
                StreamReader sr = new StreamReader("C:/phrase-sets/mackenzie-set.txt");
                String line = sr.ReadLine();
                while (line != null)
                {
                    phrases.Add(line, new Phrase(line, line));
                    validChoices.Add(line);
                    line = sr.ReadLine();
                }
                sr.Close();

                UnityEngine.Debug.Log("Loaded " + phrases.Count + " phrases from txt file with valid choices = " + validChoices.Count);

                String message = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                    "Condition", "Participant", "ID",
                    "TranscribedText",
                    "InputStream",
                    "PresentedText",
                    "WPM", "DurationToFirstKey", "DurationTyping", "INF", "C", "F", "IF", "MSDER", "TotalErrorRate", "NotCorrectedErrorRate", "CorrectedErrorRate", "FirstKeyCorrect");

                LogWriter.TextEntryLog.WriteToLog(message);
            }

            public Phrase getNextRandomPhrase(){
                if (validChoices.Count == 0){
                    // we've ran out of phrases lol
                    Debug.LogError("Ran out of phrases! Repopulating valid choices..");
                    reset();
                }

                int index = r.Next(0, validChoices.Count);
                String phraseid = validChoices[index];
                Phrase phrase = phrases[phraseid];
                Debug.Log("Validchoices before " + validChoices.Count);
                Debug.Log("Removing " + phraseid); 
                validChoices.Remove(phraseid);
                Debug.Log("Validchoices after " + validChoices.Count);
                Debug.Log(validChoices);

                return phrase;
            }

            void reset()
            {
                foreach (String k in phrases.Keys)
                {
                    validChoices.Add(k);
                    phrases[k].hasBeenShown = false;
                }
            }

            public void test()
            {
                for (int i = 0; i < phrases.Count * 2.1; i++)
                {
                    Phrase test = getNextRandomPhrase();
                    Debug.Log("i " + i + " phrase " + test.ToString());
                }
            }
        }

        class Phrase
        {
            public String id;
            public String phrase;
            public bool hasBeenShown;
            public bool firstChar = true;
            public double firstCharAtMS = 0;
            public double phraseShownAtMS = 0;

            public Phrase(String id, String phrase)
            {
                this.id = id;
                this.phrase = phrase;
                hasBeenShown = false;
            }

            public override string ToString()
            {
                return "ID: " + id + " phrase " + phrase + " hasBeenShown " + hasBeenShown;
            }
        }
    }
}
