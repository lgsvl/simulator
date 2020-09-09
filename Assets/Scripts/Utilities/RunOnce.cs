/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Threading;

namespace Simulator.Utilities
{
    public class RunOnce : IDisposable
    {
        public bool AlreadyRunning { get { return _alreadyRunning; } private set { _alreadyRunning = value; } }
        private bool _alreadyRunning;
        private string Name;
        private Mutex Mutex;

        public RunOnce(string name)
        {
            Name = name;
            AlreadyRunning = false;
            Mutex = new Mutex(false, Name, out bool mutex);
            AlreadyRunning = !mutex;
        }

        ~RunOnce()
        {
            DisposeImpl(false);
        }

        private void DisposeImpl(bool is_disposing)
        {
            GC.SuppressFinalize(this);
            if (is_disposing)
            {
                Mutex.Close();
            }
        }

        public void Dispose()
        {
            DisposeImpl(true);
        }
    }
}
