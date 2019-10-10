using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TierIV.Event
{
    public class EventNotifier
    {
        private static EventNotifier _instance = null;

        public static EventNotifier Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventNotifier();
                }

                return _instance;
            }
        }

        private EventNotifier()
        {
        }

        public delegate void OnNotifyEvent(string text);
        event OnNotifyEvent _notifyEvent;
        Dictionary<string, OnNotifyEvent> _evt;

        public void SubscribeEvent(OnNotifyEvent notifyEventHandler, string tag = "")
        {
            _evt[tag] += notifyEventHandler;

            _notifyEvent += notifyEventHandler;
        }

        public void UnSubscribeEvent(OnNotifyEvent notifyEventHandler)
        {
            foreach (var v in _evt)
            {
                var eh = v.Value;
                eh -= notifyEventHandler;
                _evt[v.Key] = eh;
            }
        }

        public void BroadcastEvent(string value)
        {
            BroadcastEvent(null, value);
        }

        public void BroadcastEvent(string tag, string value)
        {
            List<OnNotifyEvent> handlerList = new List<OnNotifyEvent>();

            // tag == null is all message send
            if (tag == null)
            {
                // 
                handlerList.AddRange(_evt.Values);
            }
            else
            {
                handlerList.Add(_evt[tag]);
            }

            foreach (var handler in handlerList)
            {
                handler(value);
            }
        }
    }
}
