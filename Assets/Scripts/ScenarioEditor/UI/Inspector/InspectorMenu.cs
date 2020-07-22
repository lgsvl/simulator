/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Inspector
{
    using System.Collections.Generic;
    using Network.Core;
    using UnityEngine;

    /// <summary>
    /// Visual scenario editor inspector menu that can switch between different panels
    /// </summary>
    public class InspectorMenu : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Game object where all the instantiated content panels will be stored
        /// </summary>
        [SerializeField]
        private GameObject inspectorContent;

        /// <summary>
        /// Button sample in the inspector bar used for switching between content panels
        /// </summary>
        [SerializeField]
        private InspectorMenuItem buttonSample;
#pragma warning restore 0649

        /// <summary>
        /// Available inspector content panels
        /// </summary>
        private List<IInspectorContentPanel> panels = new List<IInspectorContentPanel>();

        /// <summary>
        /// Currently active inspector panel
        /// </summary>
        private IInspectorContentPanel activePanel;

        /// <summary>
        /// Unity Start method
        /// </summary>
        public void Start()
        {
            var availablePanels = inspectorContent.GetComponentsInChildren<IInspectorContentPanel>(true);
            for (var i = 0; i < availablePanels.Length; i++)
            {
                var availablePanel = availablePanels[i];
                panels.Add(availablePanel);
                availablePanel.Initialize();
                if (i == 0) availablePanel.Show();
                else availablePanel.Hide();
                var panelMenuItem = Instantiate(buttonSample, buttonSample.transform.parent);
                panelMenuItem.Setup(availablePanel);
                panelMenuItem.gameObject.SetActive(true);
            }

            buttonSample.gameObject.SetActive(false);

            activePanel = availablePanels.Length > 0 ? availablePanels[0] : null;
        }

        public void OnDestroy()
        {
            for (var i = 0; i < panels.Count; i++)
                panels[i].Deinitialize();
            panels.Clear();
        }

        /// <summary>
        /// Shows selected inspector content panel while hiding previously selected one
        /// </summary>
        /// <param name="panel"></param>
        public void ShowPanel(IInspectorContentPanel panel)
        {
            if (!panels.Contains(panel))
            {
                Log.Warning("Cannot show inspector panel which is not in the inspector content hierarchy.");
                return;
            }

            activePanel?.Hide();
            panel.Show();
            activePanel = panel;
        }
    }
}