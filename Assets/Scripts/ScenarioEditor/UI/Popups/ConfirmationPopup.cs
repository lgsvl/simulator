/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapSelecting
{
    using System;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Popup which requires user interaction to confirm an operation
    /// </summary>
    public class ConfirmationPopup : MonoBehaviour
    {
        /// <summary>
        /// Data used to setup the confirmation popup
        /// </summary>
        public class PopupData
        {
            /// <summary>
            /// Text that will be displayed on the popup
            /// </summary>
            public string Text { get; set; }
            
            /// <summary>
            /// Callback that will be invoked when user cancels operation
            /// </summary>
            public event Action CancelCallback;
            
            /// <summary>
            /// Callback that will be invoked when user confirms operation
            /// </summary>
            public event Action ConfirmCallback;

            /// <summary>
            /// Invokes the confirm callback
            /// </summary>
            public void InvokeConfirmCallback()
            {
                ConfirmCallback?.Invoke();
            }

            /// <summary>
            /// Invokes the cancel callback
            /// </summary>
            public void InvokeCancelCallback()
            {
                CancelCallback?.Invoke();
            }
        }
        
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI Text object where the text will be set
        /// </summary>
        [SerializeField]
        private Text uiText;
#pragma warning restore 0649

        /// <summary>
        /// Currently used data to display the popup
        /// </summary>
        private PopupData currentData;

        /// <summary>
        /// Can popup be shown in this moment
        /// </summary>
        public bool CanBeShown => currentData == null && !ScenarioManager.Instance.ViewsPopup;
        
        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected void OnEnable()
        {
            //Disable popup if it was shown without setup
            if (currentData == null)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the the popup if it's possible with given data
        /// </summary>
        /// <param name="popupData">Data that will be used to display popup</param>
        public void Show(PopupData popupData)
        {
            if (!CanBeShown)
            {
                popupData.InvokeCancelCallback();
                return;
            }

            currentData = popupData;
            uiText.text = currentData.Text;
            Show();
        }

        /// <summary>
        /// Shows the popup
        /// </summary>
        private void Show()
        {
            gameObject.SetActive(true);
            ScenarioManager.Instance.ViewsPopup = true;
        }

        /// <summary>
        /// Hides the popup
        /// </summary>
        private void Hide()
        {
            gameObject.SetActive(false);
            ScenarioManager.Instance.ViewsPopup = false;
        }

        /// <summary>
        /// Confirms the operation and closes the popup
        /// </summary>
        public void Confirm()
        {
            Hide();
            currentData.InvokeConfirmCallback();
            currentData = null;
        }

        /// <summary>
        /// Cancels the operation and closes the popup
        /// </summary>
        public void Cancel()
        {
            Hide();
            currentData.InvokeCancelCallback();
            currentData = null;
        }
    }
}