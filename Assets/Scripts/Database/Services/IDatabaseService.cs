/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Database.Services
{
    public interface IDatabaseService
    {
        void Open();
        void Close();
    }
}
