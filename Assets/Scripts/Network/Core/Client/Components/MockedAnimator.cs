/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Client.Components
{
    using System;
    using System.Linq;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Mocked animator component
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MockedAnimator : MockedComponentWithDeltas
    {
        /// <summary>
        /// Cached animator component reference
        /// </summary>
        private Animator cachedAnimator;

        /// <summary>
        /// Cached animator component reference
        /// </summary>
        public Animator CachedAnimator =>
            cachedAnimator ? cachedAnimator : cachedAnimator = GetComponent<Animator>();

        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedAnimator";

        /// <inheritdoc/>
        protected override void ApplySnapshot(Message message)
        {
            var parameters = CachedAnimator.parameters;
            while (message.Content.Count > 0)
            {
                var parameterName = message.Content.PopString();
                var parameterHash = Animator.StringToHash(parameterName);
                var parameter = parameters.First(param => param.nameHash == parameterHash);
                if (parameter == null)
                    continue;
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        CachedAnimator.SetFloat(parameterHash, message.Content.PopFloat());
                        break;
                    case AnimatorControllerParameterType.Int:
                        CachedAnimator.SetInteger(parameterHash, message.Content.PopInt());
                        break;
                    case AnimatorControllerParameterType.Bool:
                        CachedAnimator.SetBool(parameterHash, message.Content.PopBool());
                        break;
                }
            }
        }

        /// <inheritdoc/>
        protected override void ApplyDelta(Message message)
        {
            var commandType = message.Content.PopEnum<AnimatorCommandType>();
            switch (commandType)
            {
                case AnimatorCommandType.SetFloatById:
                    CachedAnimator.SetFloat(message.Content.PopInt(), message.Content.PopFloat());
                    break;
                case AnimatorCommandType.SetFloatByName:
                    CachedAnimator.SetFloat(message.Content.PopString(), message.Content.PopFloat());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}