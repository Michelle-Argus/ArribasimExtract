/*
* Copyright (c) Contributors, http://www.openmetaverse.org/
* See CONTRIBUTORS.TXT for a full list of copyright holders.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of the OpenSim Project nor the
*       names of its contributors may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
* 
*/
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace OpenSim.Framework.Console
{
    public enum LogPriority : int
    {
        CRITICAL,
        HIGH,
        MEDIUM,
        NORMAL,
        LOW,
        VERBOSE,
        EXTRAVERBOSE
    }

    public class LogBase
    {
        StreamWriter Log;
        public conscmd_callback cmdparser;
        public string componentname;
        private bool m_silent;

        public LogBase(string LogFile, string componentname, conscmd_callback cmdparser, bool silent)
        {
            this.componentname = componentname;
            this.cmdparser = cmdparser;
            this.m_silent = silent;
            System.Console.WriteLine("Creating new local console");

            if (String.IsNullOrEmpty(LogFile))
            {
                LogFile = componentname + ".log";
            }

            System.Console.WriteLine("Logs will be saved to current directory in " + LogFile);
            Log = File.AppendText(LogFile);
            Log.WriteLine("========================================================================");
            Log.WriteLine(componentname + " Started at " + DateTime.Now.ToString());
        }

        public void Close()
        {
            Log.WriteLine("Shutdown at " + DateTime.Now.ToString());
            Log.Close();
        }

        [Obsolete("Log.WriteLine is obsolete, use Warn / Error / Verbose instead.")]
        public void Write(string format, params object[] args)
        {
            // HOUSEKEEPING : Will remove once use is removed.
            Notice(format, args);
            return;
        }

        [Obsolete("Log.WriteLine is obsolete, use Warn / Error / Verbose instead.")]
        public void WriteLine(LogPriority importance, string format, params object[] args)
        {
            // HOUSEKEEPING : Will remove once use is removed.
            Log.WriteLine(format, args);
            Log.Flush();
            if (!m_silent)
            {
                System.Console.WriteLine(format, args);
            }
            return;
        }

        /// <summary>
        /// Sends a warning to the current log output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Warn(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Yellow, format, args);
            return;
        }

        /// <summary>
        /// Sends a warning to the current log output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Warn(string sender, string format, params object[] args)
        {
            sender = sender.ToUpper();
            int colIdx = (sender.GetHashCode() % 6) + 9;
            ConsoleColor col = (ConsoleColor)colIdx;

            WritePrefixLine(col, sender);
            WriteNewLine(ConsoleColor.Yellow, format, args);
            return;
        }

        /// <summary>
        /// Sends a notice to the current log output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Notice(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.White, format, args);
            return;
        }

        /// <summary>
        /// Sends a notice to the current log output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Notice(string sender, string format, params object[] args)
        {
            sender = sender.ToUpper();
            int colIdx = (sender.GetHashCode() % 6) + 9;
            ConsoleColor col = (ConsoleColor)colIdx;

            WritePrefixLine(col, sender);
            WriteNewLine(ConsoleColor.White, format, args);
            return;
        }

        /// <summary>
        /// Sends an error to the current log output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Error(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Red, format, args);
            return;
        }

        /// <summary>
        /// Sends an error to the current log output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Error(string sender, string format, params object[] args)
        {
            sender = sender.ToUpper();
            int colIdx = (sender.GetHashCode() % 6) + 9;
            ConsoleColor col = (ConsoleColor)colIdx;

            WritePrefixLine(col, sender);
            WriteNewLine(ConsoleColor.Red, format, args);
            return;
        }

        /// <summary>
        /// Sends a informational message to the current log output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Verbose(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Gray, format, args);
            return;
        }

        /// <summary>
        /// Sends an informational message to the current log output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Verbose(string sender, string format, params object[] args)
        {
            sender = sender.ToUpper();
            int colIdx = (sender.GetHashCode() % 6) + 9;
            ConsoleColor col = (ConsoleColor)colIdx;

            WritePrefixLine(col, sender);
            WriteNewLine(ConsoleColor.Gray, format, args);
            return;
        }

        /// <summary>
        /// Sends a status message to the current log output
        /// </summary>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Status(string format, params object[] args)
        {
            WriteNewLine(ConsoleColor.Blue, format, args);
            return;
        }

        /// <summary>
        /// Sends a status message to the current log output
        /// </summary>
        /// <param name="sender">The module that sent this message</param>
        /// <param name="format">The message to send</param>
        /// <param name="args">WriteLine-style message arguments</param>
        public void Status(string sender, string format, params object[] args)
        {
            sender = sender.ToUpper();
            int colIdx = (sender.GetHashCode() % 6) + 9;
            ConsoleColor col = (ConsoleColor)colIdx;

            WritePrefixLine(col, sender);
            WriteNewLine(ConsoleColor.Blue, format, args);
            return;
        }


        private void WriteNewLine(ConsoleColor color, string format, params object[] args)
        {
            Log.WriteLine(format, args);
            Log.Flush();
            if (!m_silent)
            {
                try
                {
                    if (color != ConsoleColor.White) 
                        System.Console.ForegroundColor = color;

                    System.Console.WriteLine(format, args);
                    System.Console.ResetColor();
                }
                catch (ArgumentNullException)
                {
                    // Some older systems dont support coloured text.
                    System.Console.WriteLine(format, args);
                }
            }
            return;
        }

        private void WritePrefixLine(ConsoleColor color, string sender)
        {
            sender = sender.ToUpper();
            Log.WriteLine("[" + sender + "] ");
            Log.Flush();

            System.Console.Write("[");

            if (!m_silent)
            {
                try
                {
                    System.Console.ForegroundColor = color;
                    System.Console.Write(sender);
                    System.Console.ResetColor();
                }
                catch (ArgumentNullException)
                {
                    // Some older systems dont support coloured text.
                    System.Console.WriteLine(sender);
                }
            }

            System.Console.Write("] \t");

            return;
        }


        public string ReadLine()
        {
            string TempStr = System.Console.ReadLine();
            Log.WriteLine(TempStr);
            return TempStr;
        }

        public int Read()
        {
            int TempInt = System.Console.Read();
            Log.Write((char)TempInt);
            return TempInt;
        }

        public IPAddress CmdPromptIPAddress(string prompt, string defaultvalue)
        {
            IPAddress address;
            string addressStr;

            while (true)
            {
                addressStr = MainLog.Instance.CmdPrompt(prompt, defaultvalue);
                if (IPAddress.TryParse(addressStr, out address))
                {
                    break;
                }
                else
                {
                    MainLog.Instance.Error("Illegal address. Please re-enter.");
                }
            }

            return address;
        }

        public int CmdPromptIPPort(string prompt, string defaultvalue)
        {
            int port;
            string portStr;

            while (true)
            {
                portStr = MainLog.Instance.CmdPrompt(prompt, defaultvalue);
                if (int.TryParse(portStr, out port))
                {
                    if (port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
                    {
                        break;
                    }
                }

                MainLog.Instance.Error("Illegal address. Please re-enter.");
            }

            return port;
        }

        // Displays a prompt and waits for the user to enter a string, then returns that string
        // Done with no echo and suitable for passwords
        public string PasswdPrompt(string prompt)
        {
            // FIXME: Needs to be better abstracted
            Log.WriteLine(prompt);
            this.Notice(prompt);
            ConsoleColor oldfg = System.Console.ForegroundColor;
            System.Console.ForegroundColor = System.Console.BackgroundColor;
            string temp = System.Console.ReadLine();
            System.Console.ForegroundColor = oldfg;
            return temp;
        }

        // Displays a command prompt and waits for the user to enter a string, then returns that string
        public string CmdPrompt(string prompt)
        {
            this.Notice(String.Format("{0}: ", prompt));
            return this.ReadLine();
        }

        // Displays a command prompt and returns a default value if the user simply presses enter
        public string CmdPrompt(string prompt, string defaultresponse)
        {
            string temp = CmdPrompt(String.Format("{0} [{1}]", prompt, defaultresponse));
            if (temp == "")
            {
                return defaultresponse;
            }
            else
            {
                return temp;
            }
        }

        // Displays a command prompt and returns a default value, user may only enter 1 of 2 options
        public string CmdPrompt(string prompt, string defaultresponse, string OptionA, string OptionB)
        {
            bool itisdone = false;
            string temp = CmdPrompt(prompt, defaultresponse);
            while (itisdone == false)
            {
                if ((temp == OptionA) || (temp == OptionB))
                {
                    itisdone = true;
                }
                else
                {
                    Notice("Valid options are " + OptionA + " or " + OptionB);
                    temp = CmdPrompt(prompt, defaultresponse);
                }
            }
            return temp;
        }

        // Runs a command with a number of parameters
        public Object RunCmd(string Cmd, string[] cmdparams)
        {
            cmdparser.RunCmd(Cmd, cmdparams);
            return null;
        }

        // Shows data about something
        public void ShowCommands(string ShowWhat)
        {
            cmdparser.Show(ShowWhat);
        }

        public void MainLogPrompt()
        {
            string[] tempstrarray;
            string tempstr = this.CmdPrompt(this.componentname + "# ");
            tempstrarray = tempstr.Split(' ');
            string cmd = tempstrarray[0];
            Array.Reverse(tempstrarray);
            Array.Resize<string>(ref tempstrarray, tempstrarray.Length - 1);
            Array.Reverse(tempstrarray);
            string[] cmdparams = (string[])tempstrarray;
            RunCmd(cmd, cmdparams);
        }

        public string LineInfo
        {
            get
            {
                string result = String.Empty;
                
                string stacktrace = Environment.StackTrace;
                List<string> lines = new List<string>(stacktrace.Split(new string[] { "at " }, StringSplitOptions.None));

                if (lines.Count > 4)
                {
                    lines.RemoveRange(0, 4);

                    string tmpLine = lines[0];

                    int inIndex = tmpLine.IndexOf(" in ");

                    if (inIndex > -1)
                    {
                        result = tmpLine.Substring(0, inIndex);

                        int lineIndex = tmpLine.IndexOf(":line ");

                        if (lineIndex > -1)
                        {
                            lineIndex += 6;
                            result += ", line " + tmpLine.Substring(lineIndex, (tmpLine.Length - lineIndex) - 5);
                        }
                    }
                }
                return result;
            }
        }
    }
}
