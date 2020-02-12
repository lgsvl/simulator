/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Identification
{
    using System.Collections.Generic;

    /// <summary>
    /// Simple identifier manager incrementing the guid
    /// </summary>
    public class SimpleIdManager : IIdManager
    {
        /// <summary>
        /// Next identifier that will be returned
        /// </summary>
        private int nextId;
        
        /// <summary>
        /// Stack returned identifiers so they can be reused
        /// </summary>
        private readonly Stack<int> returnedIds = new Stack<int>();
        
        /// <inheritdoc/>
        public int GetId()
        {
            return returnedIds.Count > 0 ? returnedIds.Pop() : nextId++;
        }

        /// <inheritdoc/>
        public void ReturnId(int id)
        {
            if (id == nextId - 1)
                nextId--;
            else
                returnedIds.Push(id);
        }
    }
}
