import React, {useState, useEffect, useContext, useCallback} from 'react'
import SingleSelect from '../Select/SingleSelect';
import Alert from '../Alert/Alert';
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import {getList} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';

function FormGeneral(props) {
    const [name, setName] = useState(props.name);
    const [apiOnly, setApiOnly] = useState(props.apiOnly);
    const [offScreen, setOffScreen] = useState(props.offScreen);
    const [clusterList, setClusterList] = useState();
    const [cluster, setCluster] = useState();
    const [formWarning, setFormWarning] = useState();
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext);
    const [isLoading, setIsLoading] = useState(false);

    const changeName = useCallback(ev => setName(ev.target.value));
    const changeApiOnly = useCallback(() => setApiOnly(prev => !prev));
    const changeOffScreen = useCallback(() => setOffScreen(prev => !prev));
    const changeCluster = useCallback(ev => setCluster(ev.target.value));

    useEffect(() => {
        const fetchData = async () => {
            setAlert({status: false});
            setIsLoading(true);
            const result = await getList('clusters');
            if (result.status === 200) {
                setClusterList(result.data);
                setCluster(result.data[0].id)
            } else {
                let alertMsg;
                if (result.name === "Error") {
                    alertMsg = result.message;
                } else {
                    alertMsg = `${result.statusText}: ${result.data.error}`;
                }
                setAlert({status: true, type: 'error', message: alertMsg});
            }
            setIsLoading(false);
        };

        fetchData();
    }, []);

    return (
        <div className={appCss.formCard}>
            <label className={appCss.inputLabel}>
                Simulation Name
            </label>
            <input
                required
                name="name"
                type="text"
                defaultValue={name}
                placeholder="name"
                onChange={changeName} />
            <label className={appCss.inputLabel}>
                Select Cluster
            </label><br />
            <label className={appCss.inputDescription}>
                Select cluster to run distributed simulation on several machines.
            </label>
            <SingleSelect
                data-for='cluster'
                placeholder='select a cluster'
                defaultValue={cluster}
                onChange={changeCluster}
                options={clusterList}
                label="name"
                value="id"
            />
            <br />
            <label className={appCss.inputLabel}>
                API Only
            </label><br />
            <label className={appCss.inputDescription}>
                Simulation in API only mode is fully controlled by Python API. Map, Ego Vehicle and other parameters are defuned using API.
            </label>
            <Checkbox
                checked={apiOnly}
                label={apiOnly ? "Use API to control simulation" : "Not using API to control simulation"}
                name={'apiOnly'}
                onChange={changeApiOnly}/>
            <br />
            <label className={appCss.inputLabel}>
                Headless Mode
            </label><br />
            <label className={appCss.inputDescription}>
                In Headless Mode main view is not rendered. Use this mode to optimize simulation performance when interaction is not needed.
            </label>
            <Checkbox
                checked={offScreen}
                label={offScreen ? "Runing in Headless Mode" : "Running in Normal Mode"}
                name={'offScreen'}
                disabled={props.interactive}
                onChange={changeOffScreen} />
            </div>)
}

export default FormGeneral;