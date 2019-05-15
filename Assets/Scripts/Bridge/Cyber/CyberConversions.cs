/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Google.Protobuf;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Cyber
{
    static class Conversions
    {
        public static Apollo.Drivers.CompressedImage ConvertFrom(ImageData data)
        {
            return new Apollo.Drivers.CompressedImage()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = 0.0, // TODO
                    Version = 1,
                    Status = new Apollo.Common.StatusPb()
                    {
                        ErrorCode = Apollo.Common.ErrorCode.Ok,
                    },
                },
                MeasurementTime = 0.0, // TODO
                FrameId = data.Frame,
                Format = "jpg",

                Data = ByteString.CopyFrom(data.Bytes, 0, data.Bytes.Length),
            };
        }
    }
}
