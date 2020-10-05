/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System;
    using Agents;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;

    /// <inheritdoc/>
    /// <remarks>
    /// Scenario element with variant that can be exchanged
    /// </remarks>
    public class ScenarioElementWithVariant : ScenarioElement
    {
        /// <summary>
        /// Name for the gameobject containing the model instance
        /// </summary>
        private static string modelObjectName = "Model";
        
        /// <summary>
        /// Parent source of this scenario element
        /// </summary>
        protected ScenarioElementSource source;

        /// <summary>
        /// Current variant selected for this scenario element
        /// </summary>
        protected SourceVariant variant;

        /// <summary>
        /// Cached model instance object
        /// </summary>
        protected GameObject modelInstance;

        /// <summary>
        /// Event invoked when this element changes the variant
        /// </summary>
        public event Action<SourceVariant> VariantChanged;

        /// <summary>
        /// Setup method for initializing the required element data
        /// </summary>
        /// <param name="source">Source of this variant</param>
        /// <param name="variant">This agent variant</param>
        public virtual void Setup(ScenarioElementSource source, SourceVariant variant)
        {
            this.source = source;
            this.variant = variant;
            ChangeVariant(variant, false);
        }

        /// <summary>
        /// Changes the current agent variant
        /// </summary>
        /// <param name="newVariant">New agent variant</param>
        /// <param name="registerUndo">If true, this action can be undone</param>
        public void ChangeVariant(SourceVariant newVariant, bool registerUndo = true)
        {
            var position = Vector3.zero;
            var rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            if (modelInstance != null)
            {
                position = modelInstance.transform.localPosition;
                rotation = modelInstance.transform.localRotation;
                source.ReturnModelInstance(modelInstance);
            }

            if (registerUndo)
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoChangeVariant(this, variant));
            variant = newVariant;
            modelInstance = source.GetModelInstance(variant).gameObject;
            modelInstance.name = modelObjectName;
            modelInstance.transform.SetParent(transform);
            modelInstance.transform.localPosition = position;
            modelInstance.transform.localRotation = rotation;
            VariantChanged?.Invoke(variant);
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
            if (modelInstance != null)
                source.ReturnModelInstance(modelInstance);
            Destroy(gameObject);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            var originWithVariant = origin as ScenarioElementWithVariant;
            if (originWithVariant==null)
                throw new ArgumentException($"Could not cast copied element to {nameof(ScenarioElementWithVariant)} type.");
            source = originWithVariant.source;
            variant = originWithVariant.variant;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == modelObjectName)
                    modelInstance = child.gameObject;
            }
        }
    }
}