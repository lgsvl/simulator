/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Simulator
{
    public class SimulatorConsole : MonoBehaviour
    {
        struct TabData
        {
            public ConsoleTab console;
            public Button button;
        }

        private readonly List<TabData> Tabs = new List<TabData>();

        private static SimulatorConsole instance = null;
        public TMP_InputField backlogArea;
        public Canvas consoleCanvas;
        public RectTransform tabBar;

        public Button TabButtonPrefab;

        public static BuiltinConsole Log => Instance.BuiltinConsole;

        private readonly BuiltinConsole BuiltinConsole;
        private bool RefreshDirty = false;
        private string AppendCache = string.Empty;

        public ConsoleTab currentTab;

        SimulatorConsole()
        {
            BuiltinConsole = new BuiltinConsole();
        }

        public void AddTab(ConsoleTab consoleTab)
        {
            var tabButton = Instantiate(TabButtonPrefab, Vector3.zero, Quaternion.identity, tabBar);
            Tabs.Add(new TabData
            {
                console = consoleTab,
                button = tabButton
            });
            if (Tabs.Count == 1)
            {
                ChangeTab(consoleTab);
            }

            tabButton.onClick.AddListener(() => this.ChangeTab(consoleTab));
            tabButton.gameObject.GetComponentInChildren<Text>().text = consoleTab.Name;
            Subscribe(consoleTab);
        }

        public void RemoveTab(ConsoleTab consoleTab)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].console == consoleTab)
                {
                    Destroy(Tabs[i].button.gameObject);
                    Tabs.RemoveAt(i);
                    ChangeTab(Tabs.Last().console);
                    Unsubscribe(consoleTab);
                    break;
                }
            }
        }

        void Subscribe(ConsoleTab consoleTab)
        {
            consoleTab.AppendOutput += HandleAppendOutput;
            consoleTab.RefreshContent += HandleRefreshContent;
            consoleTab.Bell += HandleBell;
        }

        void Unsubscribe(ConsoleTab consoleTab)
        {
            consoleTab.AppendOutput -= HandleAppendOutput;
            consoleTab.RefreshContent -= HandleRefreshContent;
            consoleTab.Bell -= HandleBell;
        }

        public static SimulatorConsole Instance
        {
            get
            {
                if (instance == null)
                {
                    var prefab = (GameObject)Resources.Load("Console");
                    var go = Instantiate(prefab);
                    DontDestroyOnLoad(go);
                    instance = go.GetComponent<SimulatorConsole>();
                }
                return instance;
            }
        }

        public bool Visible
        {
            get => consoleCanvas.enabled;
            set
            {
                consoleCanvas.enabled = value;
                if (value)
                {
                    RefreshContent();
                    backlogArea.ActivateInputField();
                    ScrollToEnd();
                }
                else
                {
                    backlogArea.DeactivateInputField();
                }
            }
        }

        public void ScrollToEnd()
        {
            backlogArea.MoveTextEnd(false);
            backlogArea.caretPosition = backlogArea.textComponent.textInfo.characterCount - 1;
        }

        public void ChangeTab(ConsoleTab tab)
        {
            currentTab = tab;
            SetTabTitle(tab, tab.Name);
            if (Visible)
            {
                backlogArea.Select();
                RefreshContent();
                ScrollToEnd();
            }
        }

        void Start()
        {
            AddTab(BuiltinConsole);
        }

        void OnEnable()
        {
            backlogArea.onValidateInput += FilterInput;
            //backlogArea.onSelect.AddListener(Refocus);
            ScrollToEnd();
            Application.logMessageReceived += BuiltinConsole.LogMessage;
            foreach (var tab in Tabs)
            {
                Subscribe(tab.console);
            }
        }

        void OnDisable()
        {
            backlogArea.onValidateInput -= FilterInput;
            Application.logMessageReceived -= BuiltinConsole.LogMessage;
            foreach (var tab in Tabs)
            {
                Unsubscribe(tab.console);
            }
        }

        void OnGUI()
        {
            if (!Event.current.isKey)
                return;

            if (Event.current.keyCode == KeyCode.Escape && Event.current.type == EventType.KeyDown)
            {
                Event.current.Use();
                Visible = !Visible;
            }

            if (Visible)
            {
                Debug.developerConsoleVisible = false; // TODO Hack because unity won't disable on dev builds

                bool hasSelection = backlogArea.selectionFocusPosition != backlogArea.selectionAnchorPosition;

                switch (Event.current.keyCode)
                {
                    case KeyCode.Backspace:
                    case KeyCode.Delete:
                    case KeyCode.DownArrow:
                    case KeyCode.LeftArrow:
                    case KeyCode.RightArrow:
                    case KeyCode.UpArrow:
                        if (Event.current.type == EventType.KeyDown)
                        {
                            currentTab.HandleSpecialKey(Event.current.keyCode);
                        }

                        Event.current.Use();
                        break;
                    case KeyCode.C:
                        if (Event.current.control && !hasSelection)
                        {
                            if (Event.current.type == EventType.KeyDown)
                            {
                                currentTab.HandleCharReceived('\u0003');
                            }

                            Event.current.Use();
                        }
                        break;
                    case KeyCode.D:
                        if (Event.current.control)
                        {
                            if (Event.current.type == EventType.KeyDown)
                            {
                                currentTab.HandleCharReceived('\u0004');
                            }

                            Event.current.Use();
                        }
                        break;
                    case KeyCode.W:
                        if (Event.current.control)
                        {
                            if (Event.current.type == EventType.KeyDown)
                            {
                                currentTab.HandleCharReceived('\u0017');
                            }

                            Event.current.Use();
                        }
                        break;
                    case KeyCode.X:
                        if (Event.current.control)
                        {
                            if (Event.current.type == EventType.KeyDown)
                            {
                                currentTab.HandleCharReceived('\u0018');
                            }

                            Event.current.Use();
                        }
                        break;
                }
            }
        }

        public void HandleRefreshContent(ConsoleTab tab)
        {
            if (tab == currentTab)
            {
                RefreshDirty = true;
            }
        }

        public void SetTabTitle(ConsoleTab tab, string title)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].console == tab)
                {
                    Tabs[i].button.gameObject.GetComponentInChildren<Text>().text = title;
                    break;
                }
            }
        }

        public void HandleBell(ConsoleTab tab)
        {
            if (currentTab != tab)
            {
                SetTabTitle(tab, tab.Name + " !");
            }
        }

        public void RefreshContent()
        {
            backlogArea.lineLimit = currentTab.backlogCapacity;
            backlogArea.text = string.Join("\n", currentTab.Backlog);
            AppendCache = string.Empty;
            backlogArea.caretPosition = backlogArea.textComponent.textInfo.characterCount - 1;
        }

        private char FilterInput(string text, int charIndex, char addedChar)
        {
            currentTab.HandleCharReceived(addedChar);
            ScrollToEnd();
            return '\0';
        }

        public void HandleAppendOutput(ConsoleTab tab, string output)
        {
            if (tab == currentTab)
            {
                AppendCache += output;
            }
        }

        public void AppendOutput(string output)
        {
            backlogArea.text += output;
            backlogArea.caretPosition = backlogArea.textComponent.textInfo.characterCount - 1;
        }

        void Update()
        {
            if (RefreshDirty)
            {
                RefreshDirty = false;
                RefreshContent();
            }

            if (AppendCache.Length > 0)
            {
                AppendOutput(AppendCache);
                AppendCache = string.Empty;
            }
        }
    }

    public abstract class ConsoleTab
    {
        public int backlogCapacity = 500;
        public string Name;
        public List<string> Backlog = new List<string> { string.Empty };

        public event Action<ConsoleTab, string> AppendOutput = delegate { };
        public event Action<ConsoleTab> RefreshContent = delegate { };
        public event Action<ConsoleTab> Bell = delegate { };

        internal abstract void HandleCharReceived(char addedChar);
        internal abstract void HandleSpecialKey(KeyCode keyCode);

        public void OnBell()
        {
            Bell.Invoke(this);
        }

        public void Backspace()
        {
            var lastLine = Backlog[Backlog.Count - 1];
            if (lastLine.Length == 0)
                return;

            Backlog[Backlog.Count - 1] = lastLine.Remove(lastLine.Length - 1);
            RefreshContent.Invoke(this);
        }

        public void Append(string output)
        {
            AppendOutput.Invoke(this, output);
            var previousIndex = output.IndexOf('\n');
            if (previousIndex == -1)
            {
                Backlog[Backlog.Count - 1] += output;
                return;
            }

            var lines = output.Split('\n');
            Backlog[Backlog.Count - 1] += lines[0];
            Backlog.AddRange(lines.Skip(1));
            if (Backlog.Count > backlogCapacity)
            {
                Backlog.RemoveRange(0, Backlog.Count - backlogCapacity);
                RefreshContent.Invoke(this);
            }
        }

        public void WriteLine(string line)
        {
            Backlog[Backlog.Count - 1] += line;
            Backlog.Add(string.Empty);
            if (Backlog.Count <= backlogCapacity)
            {
                AppendOutput.Invoke(this, line + "\n");
            }
            else
            {
                Backlog.RemoveRange(0, Backlog.Count - backlogCapacity);
                RefreshContent.Invoke(this);
            }
        }
    }
}
