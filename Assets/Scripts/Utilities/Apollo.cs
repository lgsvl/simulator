/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Utilities
{
    public static class Apollo
    {
        public interface IOneOf
        {
            KeyValuePair<string, object> GetOne();
        }

        public interface IOneOf<T> : IOneOf where T : IOneOf<T> { }
    }
}
