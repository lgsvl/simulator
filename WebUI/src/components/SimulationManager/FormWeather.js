import React, {useCallback, useContext, useState} from 'react'
import appCss from '../../App/App.module.less';
import css from './SimulationManager.module.less';
import { SimulationContext } from "../../App/SimulationContext";
import DatePicker from 'react-datepicker';
import "react-datepicker/dist/react-datepicker.css";

function FormWeather() {
    const [simulation, setSimulation] = useContext(SimulationContext);
    const {weather} = simulation;
    let {rain, wetness, fog, cloudiness} = weather || {};
    const [formWarning, setFormWarning] = useState('');
    function validNumberInput(val, min, max, ) {
        return !isNaN(val) && val >= min && val <= max;
    }
    function adjustTime(datetime) {
        const timestamp = datetime.valueOf() + datetime.getTimezoneOffset()*60*1000;
        return new Date(timestamp);
    }
    const changeTimeOfDay = useCallback(datetime => {
        const timestamp = datetime.valueOf() - datetime.getTimezoneOffset()*60*1000;
        const adjusted = new Date(timestamp);
        setSimulation({...simulation, timeOfDay: adjusted.toISOString() });
    });
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
            <h4 className={appCss.inputLabel}>
                Time of day
            </h4>
            <p className={appCss.inputDescription}>
                Set time of day during simulation.
            </p>
            <div>
                <DatePicker
                    selected={adjustTime(new Date(simulation.timeOfDay) || new Date())}
                    onChange={changeTimeOfDay}
                    showTimeSelect
                    showTimeSelectOnly
                    timeFormat="HH:mm"
                    timeIntervals={30}
                    dateFormat="HH:mm"
                    timeCaption="Time"
                />
            </div>
            <div className={css.weatherInput}>
                <h4 className={appCss.inputLabel}>
                    Rain
                </h4>
                <p className={appCss.inputDescription}>
                    Raining introduces particle droplet effects falling from the sky and camera post post-processing effects.
                </p>
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
                <h4 className={appCss.inputLabel}>
                    Wetness
                </h4>
                <p className={appCss.inputDescription}>
                    Wetness covers the road and sidewalks with water.
                </p>
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
                <h4 className={appCss.inputLabel}>
                    Fog
                </h4>
                <p className={appCss.inputDescription}>
                    Defines amount of fog and other particles in the air.
                </p>
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
                <h4 className={appCss.inputLabel}>
                    Cloudiness
                </h4>
                <p className={appCss.inputDescription}>
                    Defines amount of clouds during simulation.
                </p>
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