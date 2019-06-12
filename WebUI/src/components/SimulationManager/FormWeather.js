import React, {useState, useCallback, useContext} from 'react'
import SingleSelect from '../Select/SingleSelect';
import Alert from '../Alert/Alert';
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import css from './SimulationManager.module.less';
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';

function FormWeather(props) {
    const [timeOfDay, setTimeOfDay] = useState(props.timeOfDay);
    const [cloudiness, setCloudiness] = useState(props.cloudiness);
    const [rain, setRain] = useState(props.rain);
    const [wetness, setWetness] = useState(props.wetness);
    const [fog, setFog] = useState(props.fog);
    const context = useContext(SimulationContext);

    const changeTimeOfDay = useCallback(ev => setTimeOfDay(ev.target.value));
    const changeCloudiness = useCallback(ev => setCloudiness(ev.target.value));
    const changeRain = useCallback(ev => setRain(ev.target.value));
    const changeWetness = useCallback(ev => setWetness(ev.target.value));
    const changeFog = useCallback(ev => setFog(ev.target.value));
    return (
        <div className={appCss.formCard}>
            <label
                className={appCss.inputLabel}>
                Time of day
            </label><br />
            <label className={appCss.inputDescription}>
                Set time of day during simulation.
            </label><br />
            <br />
            <input name="timeOfDay" type="text"
                defaultValue={timeOfDay || new Date()}
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
                    defaultValue={rain}
                    onChange={changeRain}
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
                    defaultValue={wetness}
                    onChange={changeWetness}
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
                    defaultValue={fog}
                    onChange={changeFog}
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
                    defaultValue={cloudiness}
                    onChange={changeCloudiness}
                    step="0.01"
                    placeholder="cloudiness"/>
            </div>
        </div>)
}

export default FormWeather;