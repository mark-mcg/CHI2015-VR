using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Keyboard
{
    using System;

    /// <summary>
    /// Contains approximate string matching
    /// </summary>
    public static class LevenshteinDistance
    {
        public static double MSDLD, meanLengthAlignment, oldMSD, newMSD, C;

        public static double getMSDErrorRate(string presentedText, string transcribedText){

            if (transcribedText.Length > 150)
            {
                Debug.Log("Transcribed text is getting too long, not going to bother!");
                LogWriter.TextEntryLog.WriteToLog("Transcribed text is getting too long, not going to bother!");
                return 0;
            }

            int[,] D = LevenshteinDistance.MSD(presentedText, transcribedText);
            MSDLD = getLD(presentedText, transcribedText, D);

            UnityEngine.Debug.Log("Presented text: " + presentedText);
            UnityEngine.Debug.Log("Transcribed text: " + transcribedText);

            Align(presentedText, transcribedText, D, presentedText.Length, transcribedText.Length, "", "");

            C = 0;

            for (int i = 0; i < AAout.Length; i++){
                if (AAout[i] == ABout[i])
                    C++;
            }

            //UnityEngine.Debug.Log("Count correct " + C);

            meanLengthAlignment = ((AAout.Length + ABout.Length) / 2);
            //UnityEngine.Debug.Log("Mean length of alignments is " + meanLengthAlignment);
            //UnityEngine.Debug.Log("MSDLD " + MSDLD);

            oldMSD = (MSDLD / ((double)Math.Max(presentedText.Length, transcribedText.Length))) * 100;
            //UnityEngine.Debug.Log("OLD MSD ERROR RATE  " + oldMSD);

            newMSD =  meanLengthAlignment == 0 ? 100 : (MSDLD / meanLengthAlignment) * 100;
            //UnityEngine.Debug.Log("NEW MSD ERROR RATE " + newMSD);

            return newMSD;

        }

        static string AAout, ABout;

        static void Align(string A, string B, int[,] D, int X, int Y, string AA, string AB)
        {
            //UnityEngine.Debug.Log("aliiiiign");

            if (X == 0 && Y == 0)
            {
                AAout = String.Copy(AA);
                ABout = String.Copy(AB);
                return;
            }

            if (X > 0 && Y > 0)
            {
                if (D[X,Y] == D[X - 1,Y - 1] && A[X - 1] == B[Y - 1])
                    Align(A, B, D, X - 1, Y - 1, A[X - 1] + AA, B[Y - 1] + AB);
                if (D[X,Y] == D[X - 1,Y - 1] + 1)
                    Align(A, B, D, X - 1, Y - 1, A[X - 1] + AA, B[Y - 1] + AB);
            }

            if (X > 0 && D[X,Y] == D[X - 1,Y] + 1)
                Align(A, B, D, X - 1, Y, A[X - 1] + AA, "-" + AB);
            if (Y > 0 && D[X,Y] == D[X,Y - 1] + 1)
                Align(A, B, D, X, Y - 1, "-" + AA, B[Y - 1] + AB);

            return;
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        /// 
        public static int getLD(string s, string t, int[,] d)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            return d[n, m];
        }

        public static int[,] MSD(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return null;
            }

            if (m == 0)
            {
                return null;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d; // return d[n, m];
        }
    }

}
