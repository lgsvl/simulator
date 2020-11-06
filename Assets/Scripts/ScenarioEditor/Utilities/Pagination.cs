/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Limits the selected elements array to a page with fixed size
    /// </summary>
    /// <typeparam name="T">Type of the handled elements</typeparam>
    public class Pagination<T> where T : class
    {
        /// <summary>
        /// Array of all the elements that will be viewed
        /// </summary>
        private T[] elements;

        /// <summary>
        /// Elements that are currently viewed on page
        /// </summary>
        private T[] currentPageElements;

        /// <summary>
        /// Count of elements viewed on a single page
        /// </summary>
        private int elementsPerPage = 5;

        /// <summary>
        /// Count of all available pages
        /// </summary>
        private int pagesCount;

        /// <summary>
        /// Currently viewed page number, starting from 0
        /// </summary>
        private int currentPage = -1;

        /// <summary>
        /// Event invoked when all the elements were changed
        /// </summary>
        public event Action ChangedAllElements;

        /// <summary>
        /// Event invoked when current page changes with page number and currently viewed elements
        /// </summary>
        public event Action<int, T[]> PageChanged;
        
        /// <summary>
        /// Count of all available pages
        /// </summary>
        public int PagesCount => pagesCount;

        /// <summary>
        /// Currently viewed page number, starting from 0
        /// </summary>
        public int CurrentPage => currentPage;

        /// <summary>
        /// Setups the pagination with elements
        /// </summary>
        /// <param name="allElements">All the elements that can be viewed on pages</param>
        /// <param name="elementsPerSinglePage">Elements count viewed on a single page</param>
        public void Setup(IEnumerable<T> allElements, int elementsPerSinglePage)
        {
            elementsPerPage = elementsPerSinglePage;
            elements = allElements as T[] ?? allElements.ToArray();
            currentPageElements = new T[elementsPerSinglePage];
            pagesCount = Mathf.CeilToInt((float) elements.Length / elementsPerPage);
            ChangedAllElements?.Invoke();
            ChangePage(0);
        }

        /// <summary>
        /// Clears the pagination setup
        /// </summary>
        public void Clear()
        {
            elements = null;
            currentPage = -1;
        }

        /// <summary>
        /// Changed the current page, clamps to pages limits
        /// </summary>
        /// <param name="newPage">New page number, starting from 0</param>
        public void ChangePage(int newPage)
        {
            newPage = Mathf.Clamp(newPage, 0, PagesCount - 1);
            if (CurrentPage == newPage)
                return;
            currentPage = newPage;
            var startElement = CurrentPage * elementsPerPage;
            for (var i = 0; i < elementsPerPage; i++)
                currentPageElements[i] = startElement + i < elements.Length ? elements[startElement + i] : null;
            PageChanged?.Invoke(CurrentPage, currentPageElements);
        }

        /// <summary>
        /// Views previous page if it is possible
        /// </summary>
        public void PreviousPage()
        {
            ChangePage(CurrentPage-1);
        }
        
        /// <summary>
        /// Views next page if it is possible
        /// </summary>
        public void NextPage()
        {
            ChangePage(CurrentPage+1);
        }
    }
}