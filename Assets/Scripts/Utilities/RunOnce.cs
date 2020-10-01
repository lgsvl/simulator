/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Diagnostics;
using System.IO;

namespace Simulator.Utilities
{
    public class RunOnce
    {
        public bool AlreadyRunning { get; private set; }

        // Current implementation does not handel all possible exceptions of files.
        // It assumes there is no problem for accesse permission, disk space, etc.
        // If this causes any problems, we can refine this implementation by adding
        // more exception handlings.
        public RunOnce(string pidFileName)
        {
            if (File.Exists(pidFileName))
            {
                var pidStrings = File.ReadAllLines(pidFileName);
                if (pidStrings.Length != 1)
                {
                    UnityEngine.Debug.LogError("PID file contains more than one line!");
                }
                int id = int.Parse(pidStrings[0]);
                try
                {
                    Process.GetProcessById(id);
                    AlreadyRunning = true;
                }
                catch (Exception e)
                {
                    // Process is not active, delete PID file.
                    File.Delete(pidFileName);
                }
            }
            if (!AlreadyRunning)
            {
                Process currentProcess = Process.GetCurrentProcess();
                int pid = currentProcess.Id;
                string[] pidStrings = new string[1];
                pidStrings[0] = pid.ToString();
                Directory.CreateDirectory(Path.GetDirectoryName(pidFileName));
                File.WriteAllLines(pidFileName, pidStrings);
            }
        }
    }
}
