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
            setSimulation({...simulation, seed: Math.floor(Math.random() * Number.MAX_SAFE_INTEGER) + 1});
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
            <label className={appCss.inputLabel}>
                Use Predefined Seed
            </label><br />
            <label className={appCss.inputDescription}>
                Using pre-defined random seed makes simulation deterministic. Vehicle colors, traffic behavioral decisions and other randomized events will happen the same way while using the same seed.
            </label>
            <Checkbox
                name={'hasSeed'}
                checked={hasSeed}
                onChange={changeHasSeed}
                label={hasSeed ? "Use predefiend seed" : "Use different random seed everytime"} />
            {hasSeed && <input
                name={'seed'}
                value={seed || ''}
                onChange={changeSeed} />}
            <label className={appCss.inputLabel}>
                Enable NPC
            </label>
            <Checkbox
                name={'enableNpc'}
                checked={useTraffic}
                label={useTraffic ? "NPC is enabled" : "NPC is disabled"}
                disabled={apiOnly}
                onChange={changeUseTraffic} />
            <label className={appCss.inputLabel}>
                Random Pedestrians
            </label><br />
            <label className={appCss.inputDescription}>
                When enabled Pedestrians start to roam around randomly across the map during the simulation.
            </label>
            <Checkbox
                name={'usePedestrians'}
                checked={usePedestrians}
                label={usePedestrians ? "Pedestrians are enabled" : "Pedestrians are disabled"}
                disabled={apiOnly}
                onChange={changeusePedestrians} />
        </div>)
}

export default FormTraffic;