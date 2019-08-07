/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from "react";

export const SimulationContext = React.createContext({
    simulationEvents: null,
    mapDownloadEvents: null,
    simulation: {}
});

export const SimulationProvider = SimulationContext.Provider;
export const SimulationConsumer = SimulationContext.Consumer;