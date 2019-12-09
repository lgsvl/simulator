/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Server.Components
{
    using Client.Components;
    using Shared;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed animator component
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class DistributedAnimator : DistributedComponentWithDeltas
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
        protected override BytesStack GetSnapshot()
        {
            var bytesStack = new BytesStack();
            var parameters = CachedAnimator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        bytesStack.PushFloat(CachedAnimator.GetFloat(parameter.name));
                        bytesStack.PushString(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        bytesStack.PushInt(CachedAnimator.GetInteger(parameter.name));
                        bytesStack.PushString(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        bytesStack.PushBool(CachedAnimator.GetBool(parameter.name));
                        bytesStack.PushString(parameter.name);
                        break;
                }
            }
            return bytesStack;
        }

        /// <inheritdoc/>
        protected override void AddCorrespondingMock()
        {
            gameObject.AddComponent<MockedAnimator>();
        }

        /// <summary>
        /// Sets the parameter's float value and sends it to corresponding mocks
        /// </summary>
        /// <param name="parameterName">The parameter name</param>
        /// <param name="value">The new parameter value</param>
        public void SetFloat(string parameterName, float value)
        {
            CachedAnimator.SetFloat(parameterName, value);
            if (!IsInitialized)
                return;
            var bytesStack = new BytesStack();
            bytesStack.PushFloat(value);
            bytesStack.PushString(parameterName);
            bytesStack.PushEnum<AnimatorCommandType>((int)AnimatorCommandType.SetFloatByName);
            SendDelta(bytesStack, MessageType.ReliableSequenced);
        }
    }
}