/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Nav
{
    using Elements;
    using Managers;
    using Map;

    /// <summary>
    /// <see cref="ScenarioElement"/> implementation representing the <see cref="NavOrigin"/> in the scenario editor
    /// </summary>
    public class ScenarioNavOrigin : ScenarioElement
    {
        /// <inheritdoc/>
        public override string ElementType => "ScenarioNavOrigin";

        /// <summary>
        /// True if nav origin is imported from the map, false if it was spawned
        /// </summary>
        private bool boundToMap;

        /// <inheritdoc/>
        public override bool CanBeRotated => true;
        
        /// <inheritdoc/>
        public override bool CanBeCopied => false;

        /// <inheritdoc/>
        public override bool CanBeRemoved => !BoundToMap;

        /// <summary>
        /// True if nav origin is imported from the map, false if it was spawned
        /// </summary>
        public bool BoundToMap => boundToMap;

        /// <summary>
        /// <see cref="NavOrigin"/> that is represented by this scenario element
        /// </summary>
        public NavOrigin NavOrigin { get; private set; }

        /// <summary>
        /// Initializes this <see cref="ScenarioNavOrigin"/>
        /// </summary>
        /// <param name="navOrigin"><see cref="NavOrigin"/> that is represented by this scenario element</param>
        /// <param name="boundToMap">True if nav origin is imported from the map, false if it was spawned</param>
        public void Setup(NavOrigin navOrigin, bool boundToMap)
        {
            NavOrigin = navOrigin;
            this.boundToMap = boundToMap;
            ScenarioManager.Instance.GetExtension<ScenarioNavExtension>().RegisterNavOrigin(this);
        }

        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            ScenarioManager.Instance.GetExtension<ScenarioNavExtension>().UnregisterNavOrigin(this);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (this != null)
                DestroyImmediate(gameObject);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            if (!(origin is ScenarioNavOrigin originNav))
            {
                return;
            }

            if (NavOrigin == null)
                Setup(GetComponent<NavOrigin>(), false);
            NavOrigin.OriginX = originNav.NavOrigin.OriginX;
            NavOrigin.OriginY = originNav.NavOrigin.OriginY;
            NavOrigin.Rotation = originNav.NavOrigin.Rotation;
        }
    }
}