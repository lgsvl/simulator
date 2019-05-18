/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros
{
    static class Conversions
    {
        public static CompressedImage ConvertFrom(ImageData data)
        {
            return new CompressedImage()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(), // TODO: time should be virtual Unity time, not real world time
                    frame_id = data.Frame,
                },
                format = "jpeg",
                data = new PartialByteArray()
                {
                    Array = data.Bytes,
                    Length = data.Length,
                },
            };
        }
    }
}
