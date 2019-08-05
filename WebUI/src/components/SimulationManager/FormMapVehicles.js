import React, {useState, useEffect, useContext, useCallback} from 'react'
import SingleSelect from '../Select/SingleSelect';
import {IoIosClose, IoIosAdd} from 'react-icons/io'
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import css from './SimulationManager.module.less';
import {getList} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import Alert from '../Alert/Alert';

function FormMapVehicles() {
    const [mapList, setMapList] = useState();
    const [vehicleList, setVehicleList] = useState();
    const [simulation, setSimulation] = useContext(SimulationContext);
    const [alert, setAlert] = useState({status: false});
    let {map, vehicles, interactive, headless, apiOnly} = simulation;
    const [formWarning, setFormWarning] = useState('');
    const [showNewVehicleField, setShowNewVehicleField] = useState(false);
    const changeInteractive = useCallback(() => setSimulation(prev => ({...simulation, interactive: !prev.interactive})));
    const changeMap = useCallback(ev => setSimulation({...simulation, map: parseInt(ev.target.value)}));
    const changeVehicles = useCallback(ev => {
        const vidx = parseInt(ev.currentTarget.dataset.vidx);
        const val = parseInt(ev.currentTarget.value);
        setSimulation(prev => {
            const newArray = Array.from(prev.vehicles || []);
            if (newArray[vidx]) {
                newArray[vidx].vehicle = val;
            } else {
                newArray[vidx] = {
                    vehicle: val,
                    connection: "",
                    simulation: simulation.id
                }
            }
            return {...simulation, vehicles: newArray};
        });
        setShowNewVehicleField(false);
    });
    const changeConnection = useCallback(ev => {
        const vidx = parseInt(ev.currentTarget.dataset.vidx);
        const val = ev.currentTarget.value;
        setSimulation(prev => {
            const newArray = Array.from(prev.vehicles || []);
            if (newArray[vidx]) {
                newArray[vidx].connection = val;
            } else {
                newArray[vidx] = {
                    vehicle: 0,
                    connection: val,
                    simulation: simulation.id
                }
            }
            return {...simulation, vehicles: newArray};
        });
        setShowNewVehicleField(false);
    });

    useEffect(() => {
        // const ac = new AbortController();
        const fetchData = async () => {
            setAlert({status: false});
            // setIsLoading(true);
            const mapResult = await getList('maps');
            const vehicleResult = await getList('vehicles');
            if (mapResult.status === 200) {
                setMapList(mapResult.data);
            } else {
                let alertMsg;
                if (mapResult.name === "Error") {
                    alertMsg = mapResult.message;
                } else {
                    alertMsg = `${mapResult.statusText}: ${mapResult.data.error}`;
                }
                setAlert({status: true, type: 'error', message: alertMsg});
            }
            if (vehicleResult.status === 200) {
                setVehicleList(vehicleResult.data);
            } else {
                let alertMsg;
                if (vehicleResult.name === "Error") {
                    alertMsg = vehicleResult.message;
                } else {
                    alertMsg = `${vehicleResult.statusText}: ${vehicleResult.data.error}`;
                }
                setAlert({status: true, type: 'error', message: alertMsg});
            }
            // setIsLoading(false);
        };

        fetchData();
    }, []);

    function addVehicleField() {
        if (showNewVehicleField) {
            setFormWarning('Please use the empty field first.');
        } else {
            setShowNewVehicleField(true);
        }
    }

    function deleteVehicleField(ev) {
        const vidx = parseInt(ev.currentTarget.dataset.vidx);
        if (vidx === vehicles.length && showNewVehicleField) {
            setShowNewVehicleField(false);
            setFormWarning('');
        } else {
            setSimulation(prev => {
                prev.vehicles.splice(vidx, 1);
                const newArray = [...prev.vehicles];
                return {...simulation, vehicles: newArray};
            });
        }
    };

    function alertHide () {
        setAlert({status: false});
    }

    function bridgeName(vehicleList, vehicleId) {
        if (!vehicleList)
            return 'No Bridge';

        let vehicle = vehicleList.filter(v => v.id == vehicleId);
        if (!vehicle)
            return 'No Bridge';

        if (!('bridgeType' in vehicle[0]) || !vehicle[0].bridgeType)
            return 'No Bridge';

        return vehicle[0].bridgeType + ' connection';
    }

    return (
        <div className={appCss.formCard}>
            {
                alert.status &&
                <Alert type={alert.alertType} msg={alert.alertMsg}>
                    <IoIosClose onClick={alertHide} />
                </Alert>
            }
            <div>
                <h4 className={appCss.inputLabel}>
                    Select Map
                </h4>
                <p className={appCss.inputDescription}>
                    Select 3D environment for simulation.
                </p>
                <SingleSelect
                    data-for='map'
                    placeholder='select a map'
                    defaultValue={map || 'DEFAULT'}
                    onChange={changeMap}
                    options={mapList}
                    label="name"
                    value="id"
                    disabled={apiOnly}
                />
                <br />
                <br />
                <h4 className={appCss.inputLabel}>
                    Select Vehicles
                </h4>
                <p className={appCss.inputDescription}>
                    Select one or multiple vehicles for simulation. Connection string could be an IP address or host name with/without port number.
                </p>
                {vehicles.length > 0 &&
                    vehicles.map((v, i) => {
                        return <div key={`connection_${i}`} className={css.connectionField}>
                            <SingleSelect
                                data-vidx={i}
                                placeholder='select a vehicle'
                                defaultValue={v.vehicle || 'DEFAULT'}
                                onChange={changeVehicles}
                                options={vehicleList}
                                label="name"
                                value="id"
                                style={{width: '45%'}}
                                disabled={apiOnly}
                            />
                            <input
                                data-vidx={i}
                                value={v.connection}
                                style={{width: '45%'}}
                                placeholder={bridgeName(vehicleList, v.vehicle)}
                                onChange={changeConnection}
                                disabled={apiOnly || bridgeName(vehicleList, v.vehicle) === bridgeName(null, null)}
                            />
                            <IoIosClose className={css.formIcons} data-vidx={i} onClick={deleteVehicleField} />
                        </div>
                    })
                }
                {   (vehicles.length === 0 || showNewVehicleField) &&
                    <div key={'connection_'} className={css.connectionField}>
                        <SingleSelect
                            data-vidx={vehicles.length}
                            placeholder='select a vehicle'
                            defaultValue='DEFAULT'
                            onChange={changeVehicles}
                            options={vehicleList}
                            label="name"
                            value="id"
                            style={{width: '45%'}}
                            disabled={apiOnly}
                        />
                        <input
                            data-vidx={vehicles.length}
                            defaultValue={''}
                            style={{width: '45%'}}
                            placeholder='Bridge connection string'
                            onChange={changeConnection}
                            disabled={true} />
                        <IoIosClose className={css.formIcons} data-vidx={vehicles.length} onClick={deleteVehicleField} />
                    </div>
                }
                <IoIosAdd className={css.formIcons} onClick={addVehicleField}/><br />
                <br />
                <h4 className={appCss.inputLabel}>
                    Interactive Mode
                </h4>
                <p className={appCss.inputDescription}>
                    Running simulation in interactive mode allows to control time flow, create snapshots interact with environment and control vehicles manually.
                </p>
                <Checkbox
                    checked={interactive}
                    label={interactive ? "Simulation will run using Interactive Mode" : "Simulation will not run using Interactive Mode"}
                    name={'interactive'}
                    disabled={apiOnly || headless}
                    onChange={changeInteractive} />
            </div>
            <span className={appCss.formWarning}>{formWarning}</span>
        </div>)
}

export default FormMapVehicles;