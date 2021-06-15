/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using Simulator.Analysis;
using System.Collections.Generic;
using System;

namespace Simulator.Sensors
{
    public abstract class SensorBase : MonoBehaviour
    {
        public enum SensorDistributionType
        {
            MainOnly = 0,
            MainOrClient = 1,
            ClientOnly = 2
        }

        protected bool isInitialized;
        public List<AnalysisReportItem> SensorAnalysisData;
        public string Name;

        public virtual List<string> SupportedBridgeTypes => new List<string>() { };

        [SensorParameter]
        public string Topic;
        [SensorParameter]
        public string Frame;

        public virtual SensorDistributionType DistributionType => SensorDistributionType.MainOnly;
        public virtual float PerformanceLoad { get; } = 0.1f;

        [HideInInspector]
        public Transform ParentTransform;

        public virtual Type GetDataBridgePlugin()
        {
            return null;
        }

        protected virtual void Start()
        {
            if (isInitialized)
                return;

            Initialize();
            isInitialized = true;
        }

        protected virtual void OnDestroy()
        {
            if (!isInitialized)
                return;

            Deinitialize();
            isInitialized = false;
        }

        protected abstract void Initialize();

        protected abstract void Deinitialize();

        public abstract void OnBridgeSetup(BridgeInstance bridge);
        public abstract void OnVisualize(Visualizer visualizer);
        public abstract void OnVisualizeToggle(bool state);
        public virtual void OnAnalyze() { }
        public virtual void SetAnalysisData() { }
    }
}
