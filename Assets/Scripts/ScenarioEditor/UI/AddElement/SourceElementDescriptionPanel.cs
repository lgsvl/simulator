/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System.Collections;
    using Agents;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Panel that displays the description for the source element
    /// </summary>
    public class SourceElementDescriptionPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Text that displays the element name as a title
        /// </summary>
        [SerializeField]
        private Text elementNameTitle;

        /// <summary>
        /// Text that displays whole element description
        /// </summary>
        [SerializeField]
        private Text elementDescriptionText;

        /// <summary>
        /// Duration of the show and hide animation in seconds
        /// </summary>
        [SerializeField]
        private float animationDuration = 0.15f;

        /// <summary>
        /// Delay after which show should be invoked (seconds since on pointer enter)
        /// </summary>
        [SerializeField]
        private float showDelay = 0.5f;
#pragma warning restore 0649

        /// <summary>
        /// Cached RectTransform for this panel
        /// </summary>
        private RectTransform rectTransform;

        /// <summary>
        /// Variant which description is currently shown
        /// </summary>
        private SourceVariant shownVariant;

        /// <summary>
        /// Delay after which show should be invoked (seconds since on pointer enter)
        /// </summary>
        public float ShowDelay => showDelay;

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            rectTransform = (RectTransform) transform;
        }


        /// <summary>
        /// Attaches to the given variant, setups UI for this variant
        /// </summary>
        /// <param name="variant">Variant which will be shown</param>
        private void AttachToVariant(SourceVariant variant)
        {
            DetachFromVariant();
            shownVariant = variant;
            if (variant == null) return;
            elementNameTitle.text = variant.Name;
            FillDescription(variant.Description);
        }

        /// <summary>
        /// Detaches from the variant
        /// </summary>
        private void DetachFromVariant()
        {
            if (shownVariant == null) return;
            elementNameTitle.text = null;
            elementDescriptionText.text = null;
            shownVariant = null;
        }

        /// <summary>
        /// Calculates position for the panel, so it will be fully visible on the Screen
        /// </summary>
        /// <param name="rt">Target RectTransform to which this panel will be attached</param>
        /// <returns>Position for the panel, so it will be fully visible on the Screen</returns>
        private Vector3 GetVisiblePosition(RectTransform rt)
        {
            var sizeDelta = rectTransform.sizeDelta;
            var halfWidth = sizeDelta.x / 2.0f;
            var halfHeight = sizeDelta.y / 2.0f;
            //Rect transform corners: bottom left, top left, top right, bottom right
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            //Try left edge
            var leftMiddle = Vector3.zero;
            leftMiddle.x = Mathf.Clamp(corners[0].x, halfWidth, Screen.width - halfWidth) - halfWidth;
            leftMiddle.y = Mathf.Clamp((corners[0].y + corners[1].y) / 2.0f, halfHeight, Screen.height - halfHeight);
            leftMiddle.z = rt.position.z;
            return leftMiddle;
        }

        /// <summary>
        /// Fills description text and calculates preferred height
        /// </summary>
        /// <param name="descriptionText">Text that will be set as the description</param>
        private void FillDescription(string descriptionText)
        {
            var descriptionRectTransform = elementDescriptionText.rectTransform;
            var preferredHeight = string.IsNullOrEmpty(descriptionText)
                ? 0.0f
                : elementDescriptionText.cachedTextGenerator.GetPreferredHeight(descriptionText,
                    elementDescriptionText.GetGenerationSettings(descriptionRectTransform.sizeDelta));
            var descriptionSize = descriptionRectTransform.sizeDelta;
            descriptionSize.y = preferredHeight;
            descriptionRectTransform.sizeDelta = descriptionSize;
            elementDescriptionText.text = descriptionText;
        }

        /// <summary>
        /// Shows the description panel for requested variant
        /// </summary>
        /// <param name="rt">Target RectTransform to which this panel will be attached</param>
        /// <param name="variant">Variant that which description will be displayed</param>
        public void Show(RectTransform rt, SourceVariant variant)
        {
            if (variant == null) return;

            AttachToVariant(variant);
            gameObject.SetActive(true);
            transform.localScale = Vector3.zero;
            StartCoroutine(ShowAnimation(rt));
        }

        /// <summary>
        /// Smooth animation for showing the description panel
        /// </summary>
        /// <param name="rt">Target RectTransform to which this panel will be attached</param>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator ShowAnimation(RectTransform rt)
        {
            yield return new WaitForEndOfFrame();

            transform.position = GetVisiblePosition(rt);
            for (float t = 0; t < 1.0f; t += Time.unscaledDeltaTime / animationDuration)
            {
                transform.localScale = Vector3.one * t;
                yield return new WaitForEndOfFrame();
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Hides this panel if given variant is the same as shown one
        /// </summary>
        /// <param name="variant">Variant, which description should be hidden</param>
        public void Hide(SourceVariant variant)
        {
            if (shownVariant == variant)
                Hide();
        }

        /// <summary>
        /// Hides the description panel
        /// </summary>
        public void Hide()
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(HideAnimation());
        }
        
        /// <summary>
        /// Smooth animation for hiding the description panel
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator HideAnimation()
        {
            yield return new WaitForEndOfFrame();

            for (float t = 1.0f; t > 0.0f; t -= Time.unscaledDeltaTime / animationDuration)
            {
                transform.localScale = Vector3.one * t;
                yield return new WaitForEndOfFrame();
            }

            transform.localScale = Vector3.zero;
            DetachFromVariant();
            gameObject.SetActive(false);
        }
    }
}