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

namespace Simulator
{
    public class BuiltinConsole : ConsoleTab
    {
        string inputBuffer = string.Empty;
        public int historyCapacity = 100;
        private List<string> commandHistory = new List<string>() { string.Empty };
        private int historyIndex = 0;
        public BuiltinConsole()
        {
            Name = "simulator";
            Append("simulator > ");
        }

        internal override void HandleCharReceived(char addedChar)
        {
            Append(addedChar.ToString());
            if (addedChar == '\n')
            {
                ExecuteCommand(inputBuffer);
                inputBuffer = "";
                Append("simulator > ");
            }
            else
            {
                inputBuffer += addedChar;
            }
        }

        internal override void HandleSpecialKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Backspace:
                    if (inputBuffer.Length > 0)
                    {
                        inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                        Backspace();
                    }
                    break;
                case KeyCode.DownArrow: NextCommand(); break;
                case KeyCode.UpArrow: PreviousCommand(); break;
            }
        }

        public void PreviousCommand()
        {
            if (commandHistory.Count == 0 || historyIndex == 0)
            {
                OnBell();
                return;
            }

            for (int i = 0; i < inputBuffer.Length; i++)
            {
                Backspace();
            }

            if (historyIndex == commandHistory.Count - 1)
            {
                commandHistory[commandHistory.Count - 1] = inputBuffer ?? string.Empty;
            }

            historyIndex--;
            inputBuffer = commandHistory[historyIndex];
            Append(inputBuffer);
        }

        public void NextCommand()
        {
            if (commandHistory.Count == 0 || historyIndex == commandHistory.Count - 1)
            {
                OnBell();
                return;
            }

            for (int i = 0; i < inputBuffer.Length; i++)
            {
                Backspace();
            }

            historyIndex++;
            inputBuffer = commandHistory[historyIndex];
            Append(inputBuffer);
        }

        public async void ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(inputBuffer))
                return;

            commandHistory[commandHistory.Count - 1] = command;
            commandHistory.Add(string.Empty);
            historyIndex = commandHistory.Count - 1;

            while (commandHistory.Count > historyCapacity)
            {
                commandHistory.RemoveAt(0);
            }

            try
            {
                if (command == "netinfo")
                {
                    PrintNetInfo();
                }
                else if (command == "bash")
                {
                    var tcrunner = GameObject.FindObjectOfType<TestCaseProcessManager>();
                    if (tcrunner == null) tcrunner = new GameObject().AddComponent<TestCaseProcessManager>();
                    var containerId = await tcrunner.RunContainer(null, "bash", "latest", new string[] { }, new Dictionary<string, string>(), "/tmp");
                    await tcrunner.GetContainerResult(containerId);
                }
                else if (command == "python")
                {
                    var tcrunner = GameObject.FindObjectOfType<TestCaseProcessManager>();
                    if (tcrunner == null) tcrunner = new GameObject().AddComponent<TestCaseProcessManager>();
                    var containerId = await tcrunner.RunContainer(null, "python", "latest", new string[] { }, new Dictionary<string, string>(), "/tmp");
                    await tcrunner.GetContainerResult(containerId);
                }
                else
                {
                    WriteLine("unkown command " + command);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                WriteLine("Exception: " + e.Message);
            }
        }

        private void PrintNetInfo()
        {
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                var properties = adapter.GetIPProperties();
                WriteLine("================================================================");
                WriteLine(adapter.Name);
                WriteLine(adapter.Description);
                WriteLine($"  Gateway Addresses........................ : {string.Join(", ", properties.GatewayAddresses.Select(a => a.ToString()))}");
                foreach (var addr in properties.UnicastAddresses)
                {
                    WriteLine($"  Unicast {addr.Address}:");
                    WriteLine($"       Mask.............................. {addr.IPv4Mask}");
                    WriteLine($"       Transient......................... {addr.IsTransient}");
                    WriteLine($"       DNS eligible...................... {addr.IsDnsEligible}");
                }
                WriteLine($"  DNS Addresses............................ : {string.Join(", ", properties.DnsAddresses.Select(a => a.ToString()))}");
                WriteLine($"  DHCP server Addresses.................... : {string.Join(", ", properties.DhcpServerAddresses.Select(a => a.ToString()))}");
                WriteLine($"  DNS suffix .............................. : {properties.DnsSuffix}");
                WriteLine($"  DNS enabled ............................. : {properties.IsDnsEnabled}");
                WriteLine($"  Dynamically configured DNS .............. : { properties.IsDynamicDnsEnabled}");
            }
        }

        public void LogMessage(string message, string stackTrace, LogType type)
        {
            var color = Color.white;
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    color = Color.red;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                default:
                    break;
            }
            WriteLine($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{message}</color>");
        }
    }
}
