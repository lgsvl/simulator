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

    public class Pagination<T> where T : class
    {
        private T[] elements;

        private T[] currentPageElements;

        private int elementsPerPage = 5;

        private int pagesCount;

        private int currentPage = -1;

        public event Action ChangedAllElements;

        public event Action<int, T[]> PageChanged;

        public int PagesCount => pagesCount;

        public int CurrentPage => currentPage;

        public void Setup(IEnumerable<T> allElements, int elementsPerSinglePage)
        {
            elementsPerPage = elementsPerSinglePage;
            elements = allElements as T[] ?? allElements.ToArray();
            currentPageElements = new T[elementsPerSinglePage];
            pagesCount = Mathf.CeilToInt((float) elements.Length / elementsPerPage);
            ChangedAllElements?.Invoke();
            ChangePage(0);
        }

        public void Clear()
        {
            elements = null;
            currentPage = -1;
        }

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

        public void PreviousPage()
        {
            ChangePage(CurrentPage-1);
        }
        
        public void NextPage()
        {
            ChangePage(CurrentPage+1);
        }
    }
}