import React, {useState, useCallback, useContext} from 'react'
import Alert from '../Alert/Alert';
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import { SimulationContext } from "../../App/SimulationContext";

function FormTraffic(props) {
    const [hasSeed, setHasSeed] = useState(props.hasSeed);
    const [seed, setSeed] = useState(props.seed);
    const [enableNpc, setEnableNpc] = useState(props.enableNpc);
    const [enablePedestrian, setEnablePedestrian] = useState(props.enablePedestrian);
    const context = useContext(SimulationContext);

    const changeHasSeed = useCallback(() => setHasSeed(prev => !prev));
    const changeSeed = useCallback(ev => setSeed(ev.target.value));
    const changeEnableNpc = useCallback(() => setEnableNpc(prev => !prev));
    const changeEnablePedestrian = useCallback(() => setEnablePedestrian(prev => !prev));
    return (
        <div className={appCss.formCard}>
            <label className={appCss.inputLabel}>
                Use Pre-defined Seed
            </label><br />
            <label className={appCss.inputDescription}>
                Using pre-defined random seed makes simulation deterministic. Vehicle colors, traffic behavioral decisions and other randomized events will happen the same way while using the same seed.
            </label>
            <Checkbox
                name={'hasSeed'}
                checked={hasSeed}
                onChange={changeHasSeed}
                label={hasSeed ? "Use pre-defiend seed" : "Use different random seed everytime"} />
            <input
                name={'seed'}
                defaultValue={seed}
                onChange={changeSeed}
                disabled={!hasSeed} />
            <label className={appCss.inputLabel}>
                Enable NPC
            </label>
            <Checkbox
                name={'enableNpc'}
                checked={enableNpc}
                label={enableNpc ? "NPC is enabled" : "NPC is disabled"}
                disabled={props.apiOnly}
                onChange={changeEnableNpc} />
            <label className={appCss.inputLabel}>
                Random Pedestrians
            </label><br />
            <label className={appCss.inputDescription}>
                When enabled Pedestrians start to roam around randomly across the map during the simulation.
            </label>
            <Checkbox
                name={'enablePedestrian'}
                checked={enablePedestrian}
                label={enablePedestrian ? "Pedestrians are enabled" : "Pedestrians are disabled"}
                disabled={props.apiOnly}
                onChange={changeEnablePedestrian} />
        </div>)
}

export default FormTraffic;