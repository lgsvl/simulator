import React, {useCallback, useContext, useState} from 'react'
import appCss from '../../App/App.module.less';
import css from './SimulationManager.module.less';
import { SimulationContext } from "../../App/SimulationContext";
function FormWeather() {
    const [simulation, setSimulation] = useContext(SimulationContext);
    const {weather} = simulation;
    let {rain, wetness, fog, cloudiness} = weather || {};
    const [formWarning, setFormWarning] = useState('');
    function validNumberInput(val, min, max, ) {
        return !isNaN(val) && val >= min && val <= max;
    }
    const changeTimeOfDay = useCallback(ev => setSimulation({...simulation, timeOfDay: ev.target.value}));
    const changeCloudiness = useCallback(ev => {
        const value = ev.target.value;
        if (!validNumberInput(value, 0, 1)) {
            setFormWarning(`Please put number between ${0} and ${1} for cloudiness.`);
        } else {
            setFormWarning('');
        }
        setSimulation({...simulation, weather: {...weather, cloudiness: parseFloat(value)}});
    });
    const changeRain = useCallback(ev => {
        const value = ev.target.value;
        if (!validNumberInput(value, 0, 1)) {
            setFormWarning(`Please put number between ${0} and ${1} for rain.`);
        } else {
            setFormWarning('');
        }
        setSimulation({...simulation, weather: {...weather, rain: parseFloat(value)}});
    });
    const changeWetness = useCallback(ev => {
        const value = ev.target.value;
        if (!validNumberInput(value, 0, 1)) {
            setFormWarning(`Please put number between ${0} and ${1} for wetness.`);
        } else {
            setFormWarning('');
        }
        setSimulation({...simulation, weather: {...weather, wetness: parseFloat(value)}});
    });
    const changeFog = useCallback(ev => {
        const value = ev.target.value;
        if (!validNumberInput(value, 0, 1)) {
            setFormWarning(`Please put number between ${0} and ${1} for fog.`);
        } else {
            setFormWarning('');
        }
        setSimulation({...simulation, weather: {...weather, fog: parseFloat(value)}});
    });

    return (
        <div className={appCss.formCard}>
            <label
                className={appCss.inputLabel}>
                Time of day
            </label><br />
            <label className={appCss.inputDescription}>
                Set time of day during simulation.
            </label>
            <br />
            <input name="timeOfDay" type="text"
                defaultValue={simulation.timeOfDay || new Date()}
                onChange={changeTimeOfDay} />
            <br />
            <div className={css.weatherInput}>
                <label
                    className={appCss.inputLabel}>
                    Rain
                </label>
                <br />
                <label
                    className={appCss.inputDescription}>
                    Raining introduces particle droplet effects falling from the sky and camera post post-processing effects.<br />
                    0.00 - means there is no rain,<br />
                    1.00 - means it is maximum raining.
                </label><br />
                <input
                    type="number"
                    name="rain"
                    defaultValue={rain || 0}
                    onChange={changeRain}
                    min="0"
                    max="1"
                    step="0.01"
                    placeholder="rain"/>
            </div>
            <div className={css.weatherInput}>
                <label
                    className={appCss.inputLabel}>
                    Wetness
                </label>
                <br />
                <label
                    className={appCss.inputDescription}>
                    Wetness covers the road and sidewalks with water.<br />
                    0.00 - means roads and sidewalks are dry,<br />
                    1.00 - means roads and sidewalks are covered with puddles.
                </label><br />
                <input
                    type="number"
                    name="wetness"
                    defaultValue={wetness || 0}
                    onChange={changeWetness}
                    min="0"
                    max="1"
                    step="0.01"
                    placeholder="wetness"/>
            </div>
            <div className={css.weatherInput}>
                <label
                    className={appCss.inputLabel}>
                    Fog
                </label>
                <br />
                <label
                    className={appCss.inputDescription}>
                    Defines amount of fog and other particles in the air.<br />
                    0.00 - means there is not fog,<br />
                    1.00 - means absolutely foggy.
                </label><br />
                <input
                    type="number"
                    name="fog"
                    defaultValue={fog || 0}
                    onChange={changeFog}
                    min="0"
                    max="1"
                    step="0.01"
                    placeholder="fog"/>
            </div>
            <div className={css.weatherInput}>
                <label
                    className={appCss.inputLabel}>
                    Cloudiness
                </label>
                <br />
                <label
                    className={appCss.inputDescription}>
                    Defines amount of clouds during simulation.<br />
                    0.00 - means sky is absolutely clear,<br />
                    1.00 - means no sky is visible behind.
                </label><br />
                <input
                    type="number"
                    name="cloudiness"
                    defaultValue={cloudiness || 0}
                    onChange={changeCloudiness}
                    min="0"
                    max="1"
                    step="0.01"
                    placeholder="cloudiness"/>
            </div>
            <span className={appCss.formWarning}>{formWarning}</span>
        </div>)
}

export default FormWeather;