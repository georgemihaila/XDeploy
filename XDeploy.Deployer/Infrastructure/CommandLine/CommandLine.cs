using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a command line.
    /// </summary>
    public class CommandLine
    {
        private Process _cmd;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine"/> class.
        /// </summary>
        public CommandLine(string baseDir)
        {
            _cmd = new Process();
            _cmd.StartInfo.FileName = "cmd.exe";
            _cmd.StartInfo.RedirectStandardInput = true;
            _cmd.StartInfo.RedirectStandardOutput = true;
            _cmd.StartInfo.CreateNoWindow = false;
            _cmd.StartInfo.UseShellExecute = false;
            _cmd.Start();
            _cmd.StandardInput.WriteLine("cd " + baseDir);
            _cmd.StandardInput.Flush();
        }

        /// <summary>
        /// Closes the command line.
        /// </summary>
        public void Close()
        {
            _cmd.StandardInput.Close();
            _cmd.WaitForExit();
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        public void Invoke(string action)
        {
            _cmd.StandardInput.WriteLine(action);
            _cmd.StandardInput.Flush();
        }
    }
}
