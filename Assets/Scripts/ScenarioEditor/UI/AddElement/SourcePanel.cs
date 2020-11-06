/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System;
    using Agents;
    using Elements;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Scenario element source panel visualize a scenario element source for adding new elements
    /// </summary>
    public class SourcePanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Count of source elements viewed per single page
        /// </summary>
        [SerializeField]
        private int elementsPerPage;
        
        /// <summary>
        /// Title text of this panel
        /// </summary>
        [SerializeField]
        private Text title;

        /// <summary>
        /// Panel that contains page control buttons
        /// </summary>
        [SerializeField]
        private GameObject pageControlPanel;
        
        /// <summary>
        /// Input field for quick switching current page
        /// </summary>
        [SerializeField]
        private InputField pageNumberInput;

        /// <summary>
        /// Text that displays pages count
        /// </summary>
        [SerializeField]
        private Text pagesCountText;

        /// <summary>
        /// Transform containing all the source element panels
        /// </summary>
        [SerializeField]
        private Transform elementsPanel;

        /// <summary>
        /// Prefab for a single source element panel
        /// </summary>
        [SerializeField]
        private SourceElementPanel elementPanelPrefab;
#pragma warning restore 0649

        /// <summary>
        /// Cached scenario element source class which is used for adding new elements from this panel
        /// </summary>
        private ScenarioElementSource source;

        /// <summary>
        /// Caches source variants prepared for viewing 
        /// </summary>
        private readonly Pagination<SourceVariant> variants = new Pagination<SourceVariant>();

        /// <summary>
        /// All the element panels used to display a single element
        /// </summary>
        private SourceElementPanel[] elementPanels;

        /// <summary>
        /// True if this source panel contains multiple pages
        /// </summary>
        public bool MultiplePages => variants.PagesCount > 1;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="source">Scenario element source class which will be used for adding new elements from this panel</param>
        public void Initialize(ScenarioElementSource source)
        {
            if (source==null)
                throw new ArgumentException("Cannot initialize source panel with null source.");
            this.source = source;
            title.text = source.ElementTypeName;

            var contentSizeFitter = GetComponent<ContentSizeFitter>();
            var verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.enabled = false;
            contentSizeFitter.enabled = false;
            
            variants.PageChanged += VariantsOnPageChanged;
            elementPanels = new SourceElementPanel[elementsPerPage];
            for (var i=0; i<elementsPerPage; i++)
                elementPanels[i] = Instantiate(elementPanelPrefab, elementsPanel);
            variants.Setup(source.Variants, elementsPerPage);
            gameObject.SetActive(variants.PagesCount>0);
            pageControlPanel.SetActive(variants.PagesCount>1);
            pagesCountText.text = variants.PagesCount.ToString();
            
            //Rebuild the UI layout
            contentSizeFitter.enabled = true;
            verticalLayoutGroup.enabled = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            variants.PageChanged -= VariantsOnPageChanged;
            for (var i = 0; i < elementsPerPage; i++)
            {
                elementPanels[i].Deinitialize();
                Destroy(elementPanels[i].gameObject);
            }

            elementPanels = null;
            variants.Clear();
        }

        /// <summary>
        /// Method invoked when the page of variants changes
        /// </summary>
        /// <param name="currentPage">Id of current page</param>
        /// <param name="pageVariants">Variants available on this page</param>
        private void VariantsOnPageChanged(int currentPage, SourceVariant[] pageVariants)
        {
            pageNumberInput.SetTextWithoutNotify((currentPage+1).ToString());
            for (var i = 0; i < elementsPerPage; i++)
            {
                elementPanels[i].Deinitialize();
                elementPanels[i].Initialize(this, source, pageVariants[i]);
            }
        }

        /// <summary>
        /// Method handling the string page input
        /// </summary>
        /// <param name="newPageString">New page number in string</param>
        public void OnPageInputChange(string newPageString)
        {
            pageNumberInput.SetTextWithoutNotify((variants.CurrentPage+1).ToString());
            if (int.TryParse(newPageString, out var newPage))
                variants.ChangePage(newPage-1);
        }

        /// <summary>
        /// Selects previous variants page
        /// </summary>
        public void PreviousPage()
        {
            variants.PreviousPage();
        }

        /// <summary>
        /// Selects next variants page
        /// </summary>
        public void NextPage()
        {
            variants.NextPage();
        }
    }
}
