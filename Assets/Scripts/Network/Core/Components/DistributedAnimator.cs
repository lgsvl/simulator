/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System;
    using System.Linq;
    using Messaging;
    using Messaging.Data;
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