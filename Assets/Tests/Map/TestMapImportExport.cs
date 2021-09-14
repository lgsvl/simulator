/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor.SceneManagement;
using NUnit.Framework;
using System.IO;
using Simulator.Editor;
using UnityEngine.TestTools;

namespace Simulator.Tests.Map
{
    public class TestMapImportExport
    {
        [Test]
        public void ExportImport()
        {
            var environments = Path.Combine(Application.dataPath, "External", "Environments");
            var temp = Path.Combine(Application.dataPath, "..", "Temp");

            LogAssert.ignoreFailingMessages = true;

            try
            {
                foreach (var path in Directory.EnumerateDirectories(environments))
                {
                    var map = Path.GetFileName(path);

                    if (map.EndsWith("@tmp"))
                    {
                        // skip dummy folders Jenkins creates
                        continue;
                    }

                    if (File.Exists(Path.Combine(path, ".skiptest")))
                    {
                        Debug.LogWarning($"Skipping {map}");
                        continue;
                    }

                    Debug.LogWarning($"****** Testing {map}");

                    var scene = EditorSceneManager.OpenScene(Path.Combine(environments, map, $"{map}.unity"));

                    // export

                    var autoware = new AutowareMapTool();
                    autoware.Export(Path.Combine(temp, $"{map}_autoware"));

                    Assert.IsFalse(scene.isDirty);

                    var apollo5 = new ApolloMapTool();
                    apollo5.Export(Path.Combine(temp, $"{map}_apollo5"));

                    Assert.IsFalse(scene.isDirty);

                    var opendrive = new OpenDriveMapExporter();
                    opendrive.Export(Path.Combine(temp, $"{map}_opendrive"));

                    Assert.IsFalse(scene.isDirty);

                    var lanelet = new Lanelet2MapExporter();
                    lanelet.Export(Path.Combine(temp, $"{map}_lanelet2"));

                    Assert.IsFalse(scene.isDirty);

                    // import

                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    var apolloImport = new ApolloMapImporter(10f, 0.5f, true);
                    apolloImport.Import(Path.Combine(temp, $"{map}_apollo5"));

                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    var opendriveImport = new OpenDriveMapImporter(10f, 0.5f, true);
                    opendriveImport.Import(Path.Combine(temp, $"{map}_opendrive"));

                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    var laneletImport = new Lanelet2MapImporter(true);
                    laneletImport.Import(Path.Combine(temp, $"{map}_lanelet2"));
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
