import React, {useState, useEffect, useContext, useCallback} from 'react';
import SingleSelect from '../Select/SingleSelect';
import Alert from '../Alert/Alert';
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import {getList} from '../../APIs.js'
import {IoIosClose} from "react-icons/io";
import { SimulationContext } from "../../App/SimulationContext";
import axios from 'axios';

function FormGeneral(props) {
    const [clusterList, setClusterList] = useState();
    const [alert, setAlert] = useState({status: false});
    const [simulation, setSimulation] = useContext(SimulationContext);
    const {name, cluster, apiOnly, headless} = simulation;
    const changeName = useCallback(ev => setSimulation({...simulation, name: ev.target.value}));
    const changeApiOnly = useCallback(() => setSimulation(prev => ({...simulation, apiOnly: !prev.apiOnly})));
    const changeHeadless = useCallback(() => setSimulation(prev => ({...simulation, headless: !prev.headless})));
    const changeCluster = useCallback(ev => setSimulation({...simulation, cluster: parseInt(ev.target.value)}));
    let source = axios.CancelToken.source();
    let unmounted;
    useEffect(() => {
        unmounted = false;
        const fetchData = async () => {
            setAlert({status: false});
            const result = await getList('clusters', source.token);
            if (unmounted) return;
            if (result.status === 200) {
                setClusterList(result.data);
                setSimulation({...simulation, cluster: result.data[0].id});
            } else {
                let alertMsg;
                if (result.name === "Error") {
                    alertMsg = result.message;
                } else {
                    alertMsg = `${result.statusText}: ${result.data.error}`;
                }
                setAlert({status: true, type: 'error', message: alertMsg});
            }
        };

        fetchData();
        return () => {
            unmounted = true;
            source.cancel('Cancelling in cleanup.')
        };
    }, []);

    function alertHide () {
        setAlert({status: false});
    }

    return (
        <div className={appCss.formCard}>
            {
                alert.status &&
                <Alert type={alert.alertType} msg={alert.alertMsg}>
                    <IoIosClose onClick={alertHide} />
                </Alert>
            }
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
                checked={headless}
                label={headless ? "Runing in Headless Mode" : "Running in Normal Mode"}
                name={'headless'}
                disabled={props.interactive}
                onChange={changeHeadless} />
            </div>)
}

export default FormGeneral;