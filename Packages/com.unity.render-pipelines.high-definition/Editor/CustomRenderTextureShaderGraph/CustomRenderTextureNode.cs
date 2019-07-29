using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Serializable]
    [Title("Master", "CustomRenderTexture")]
    class CustomRenderTextureNode : MasterNode<ICustomRenderTextureSubShader>
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 1;

        public static readonly List<int> AllSlots = new List<int>()
        {
            ColorSlotId,
            AlphaSlotId,
        };

        public CustomRenderTextureNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "Custom Render Texture";

            AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.black, ColorMode.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(AllSlots, true);
        }
    }
}
