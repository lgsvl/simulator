/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;

namespace Simulator.Editor
{
    public class Test
    {
        [MenuItem("Simulator/Test...", priority = 20)]
        static void Run()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }
    }
}
