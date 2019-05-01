import React from "react";

const SimulationContext = React.createContext({
    events: null
});

export const SimulationProvider = SimulationContext.Provider;
export const SimulationConsumer = SimulationContext.Consumer;