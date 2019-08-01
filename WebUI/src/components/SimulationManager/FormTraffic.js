import React, {useCallback, useContext, useState} from 'react'
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import { SimulationContext } from "../../App/SimulationContext";

function FormTraffic() {
    const [simulation, setSimulation] = useContext(SimulationContext);
    let {seed, apiOnly, usePedestrians, useTraffic} = simulation;
    const [hasSeed, setHasSeed] = useState(!!seed);

    const changeHasSeed = useCallback(() => setHasSeed(prevHasSeed => {
        const updatedHasSeed = !prevHasSeed;
        if (updatedHasSeed) {
            setSimulation({...simulation, seed: Math.floor(Math.random() * 0x7FFFFFFF) + 1});
        } else {
            setSimulation({...simulation, seed: null});
        }
        setHasSeed(updatedHasSeed);
    }));
    const changeSeed = useCallback(ev => setSimulation({...simulation, seed: ev.target.value}));
    const changeUseTraffic = useCallback(() => setSimulation(prev => ({...simulation, useTraffic: !prev.useTraffic})));
    const changeusePedestrians = useCallback(() => setSimulation(prev => ({...simulation, usePedestrians: !prev.usePedestrians})));

    return (
        <div className={appCss.formCard}>
            <h4 className={appCss.inputLabel}>
                Use Predefined Seed
            </h4>
            <p className={appCss.inputDescription}>
                Using pre-defined random seed makes simulation deterministic. Vehicle colors, traffic behavioral decisions and other randomized events will happen the same way while using the same seed.
            </p>
            <Checkbox
                name={'hasSeed'}
                checked={hasSeed}
                onChange={changeHasSeed}
                label={hasSeed ? "Use predefiend seed" : "Use different random seed everytime"} />
            {hasSeed && <input
                name={'seed'}
                value={seed || ''}
                onChange={changeSeed} />}
            <br />
            <h4 className={appCss.inputLabel}>
                Enable NPC
            </h4>
            <Checkbox
                name={'enableNpc'}
                checked={useTraffic}
                label={useTraffic ? "NPC is enabled" : "NPC is disabled"}
                disabled={apiOnly}
                onChange={changeUseTraffic} />
            <br />
            <h4 className={appCss.inputLabel}>
                Random Pedestrians
            </h4>
            <p className={appCss.inputDescription}>
                When enabled Pedestrians start to roam around randomly across the map during the simulation.
            </p>
            <Checkbox
                name={'usePedestrians'}
                checked={usePedestrians}
                label={usePedestrians ? "Pedestrians are enabled" : "Pedestrians are disabled"}
                disabled={apiOnly}
                onChange={changeusePedestrians} />
        </div>)
}

export default FormTraffic;