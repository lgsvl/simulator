/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Threading
{
    using System;

    /// <summary>
    /// Counting semaphore used to lock access to the resource, unlocked when no locks are applied
    /// </summary>
    public class LockingSemaphore
    {
        /// <summary>
        /// Count of the semaphore locks, unlocked when equals 0
        /// </summary>
        public int Locks { get; private set; }

        /// <summary>
        /// Is the semaphore currently locked
        /// </summary>
        public bool IsLocked => Locks > 0;
    
        /// <summary>
        /// Is the semaphore currently unlocked
        /// </summary>
        public bool IsUnlocked => Locks == 0;

        /// <summary>
        /// Event invoked when the semaphore gets locked
        /// </summary>
        public event Action Locked;

        /// <summary>
        /// Event invoked when the semaphore gets unlocked
        /// </summary>
        public event Action Unlocked;
    
        /// <summary>
        /// Event invoked when the count of semaphore's locks changes
        /// </summary>
        public event Action<float> LocksCountChanged;
        

        /// <summary>
        /// Raises the time scale lock by one, time scale is set to 0.0f while locked
        /// </summary>
        public void Lock()
        {
            lock (this)
            {
                if (Locks++ != 0)
                {
                    LocksCountChanged?.Invoke(Locks);
                    return;
                }

                Locked?.Invoke();
                LocksCountChanged?.Invoke(Locks);
            }
        }

        /// <summary>
        /// Lowers the time scale lock by one, time scale value is applied when gets unlocked
        /// </summary>
        public void Unlock()
        {
            lock (this)
            {
                if (Locks == 0)
                {
                    Log.Warning("Trying to unlock already unlocked semaphore.");
                    return;
                }

                if (--Locks == 0)
                {
                    Unlocked?.Invoke();
                }

                LocksCountChanged?.Invoke(Locks);
            }
        }
    }
}
