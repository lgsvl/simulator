import React, {useState, useEffect, useContext, useCallback} from 'react'
import SingleSelect from '../Select/SingleSelect';
import {IoIosClose, IoIosAdd} from 'react-icons/io'
import Checkbox from '../Checkbox/Checkbox';
import appCss from '../../App/App.module.less';
import css from './SimulationManager.module.less';
import {getList} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';

function FormMapVehicles(props) {
    const [mapList, setMapList] = useState();
    const [interactive, setInteractive] = useState();
    const [map, setMap] = useState();
    const [vehicleList, setVehicleList] = useState();
    const [connections, setConnections] = useState([{id: 1, bridge: '123.345'}, {id: 3, bridge: '098.345'}]);
    const [formWarning, setFormWarning] = useState();
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext);
    const [isLoading, setIsLoading] = useState(false);

    const changeInteractive = useCallback(() => setInteractive(!interactive));
    const changeMap = useCallback(ev => setMap(ev.target.value));
    const changeVehicles = useCallback(ev => {
        console.log(ev.target.value)
    });

    useEffect(() => {
        const fetchData = async () => {
            setAlert({status: false});
            setIsLoading(true);
            const mapResult = await getList('maps');
            const vehicleResult = await getList('vehicles');
            if (mapResult.status === 200) {
                setMapList(mapResult.data);
                setMap(mapResult.data[0].id)
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
            setIsLoading(false);
        };

        fetchData();
    }, []);

    function addVehicleField() {
        setConnections(prev => {
            const newArray = [...prev, {id: null, bridge: ''}];
            return newArray;
        });
    }

    function deleteVehicleField(ev) {
        const cidx = ev.target.dataset.cidx;
        console.log(cidx)
        setConnections(prev => {
            prev.splice(cidx, 1);
            const newArray = [...prev];
            return newArray;
        })
    };

    return (
        <div className={appCss.formCard}>
            {!isLoading && <div>
                <label className={appCss.inputLabel}>
                    Select Map
                </label><br />
                <label className={appCss.inputDescription}>
                    Select 3D environment for simulation.
                </label>
                <SingleSelect
                    data-for='map'
                    placeholder='select a map'
                    defaultValue={map}
                    onChange={changeMap}
                    options={mapList}
                    label="name"
                    value="id"
                    disabled={props.apiOnly}
                />
                {/* {vehicleList && <MultiSelect data-for='vehicles' size={vehicleList.length} defaultValue={vehicles}
                    onChange={this.handleMultiSelectInputChange} options={vehicleList} label="name" value="id" disabled={apiOnly} />} */}
                <label className={appCss.inputLabel}>
                    Select Vehicles
                </label><br />
                <label className={appCss.inputDescription}>
                    Select one or multiple vehicles for simulation.
                </label><br />
                {connections.length > 0 &&
                    connections.map((v, i) => {
                        return <div key={`connection_${i}`} className={css.connectionField}>
                            <SingleSelect
                                data-for='vehicles'
                                placeholder='select a vehicle'
                                defaultValue={v.id}
                                onChange={changeVehicles}
                                options={vehicleList}
                                label="name"
                                value="id"
                                style={{width: '45%'}}
                            />
                            <input defaultValue={v.bridge} style={{width: '45%'}} />
                            <IoIosClose className={css.formIcons} data-cidx={i} onClick={deleteVehicleField} />
                        </div>
                    })
                }
                <IoIosAdd className={css.formIcons} onClick={addVehicleField}/><br />
                <label className={appCss.inputLabel}>
                    Interactive Mode
                </label><br />
                <label className={appCss.inputDescription}>
                    Running simulation in interactive mode allows to control time flow, create snapshots interact with environment and control vehicels manually.
                </label>
                <Checkbox
                    checked={interactive}
                    label={interactive ? "Simulation will run using Interactive Mode" : "Simulation will not run using Interactive Mode"}
                    name={'interactive'}
                    disabled={props.apiOnly || props.offScreen}
                    onChange={changeInteractive} />
            
            </div>}
        </div>)
}

export default FormMapVehicles;