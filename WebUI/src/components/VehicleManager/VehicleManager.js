/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React, {useState, useEffect, useContext, useCallback} from 'react'
import FormModal from '../Modal/FormModal';
import SingleSelect from '../Select/SingleSelect';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaWrench, FaPen, FaRegWindowClose, FaRegStopCircle, FaDownload } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem, stopDownloading, restartDownloading} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';
import axios from 'axios';

function isValidJson(json) {
    try {
        JSON.parse(json);
        return true;
    } catch (e) {
        return false;
    }
}

function VehicleManager() {
    const [items, setItems] = useState();
    const [modalOpen, setModalOpen] = useState();
    const [sensorModalOpen, setSensorModalOpen] = useState();
    const [name, setName] = useState();
    const [url, setUrl] = useState();
    const [selectedItemId, setSelectedItemId] = useState();
    const [bridgeTypes, setBridgeTypes] = useState();
    const [bridgeType, setBridgeType] = useState();
    const [sensors, setSensors] = useState();
    const [method, setMethod] = useState();
    const [formWarning, setFormWarning] = useState('');
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext);
    const [updatedVehicle, setUpdatedVehicle] = useState({progress: null, id: null});
    const [isLoading, setIsLoading] = useState(false);

    const changeName = useCallback(ev => setName(ev.target.value));
    const changeUrl = useCallback(ev => setUrl(ev.target.value));
    const changeSensors = useCallback(ev => setSensors(ev.target.value));
    const changeBridgeType = useCallback(ev => setBridgeType(ev.target.value));

    let source = axios.CancelToken.source();
    let unmounted;

    const fetchData = async () => {
        setIsLoading(true);
        const result = await getList('vehicles', source.token);
        if (unmounted) return;
        if (result.status === 200) {
            const itemsData = new Map(result.data.map(d => [d.id, d]));
            setItems(itemsData);
            setIsLoading(false);
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

    useEffect(() => {
        unmounted = false;
        fetchData();
        return () => {
            unmounted = true;
            source.cancel('Cancelling in cleanup.')
        };
    }, []);

    useEffect(() => {
        if (context && context.vehicleDownloadEvents) {
            let contextData = JSON.parse(context.vehicleDownloadEvents.data);
            switch (context.vehicleDownloadEvents.type) {
                case 'VehicleDownloadComplete':
                    fetchData();
                    break;
                case 'VehicleDownload':
                    setUpdatedVehicle(contextData);
                    break;
            }
        }
    }, [context]);

    function alertHide() {
        setAlert({status: false, type: '', message: ''})
    };

    function openAddMewModal() {
        setName('');
        setUrl('');
        setSelectedItemId(null);
        setMethod('POST');
        setModalOpen(true);
    }

    function openEdit(ev) {
        setSelectedItemId(ev.currentTarget.dataset.vehicleid);
        getItem('vehicles', ev.currentTarget.dataset.vehicleid, source.token).then(res => {
            if (res.status === 200) {
                setSelectedItemId(res.data.id);
                setName(res.data.name);
                setUrl(res.data.url);
                setSensors(res.data.sensors);
                setBridgeType(res.data.bridgeType);
                setMethod('PUT');
                setModalOpen(true);
            } else {
                setAlert({status: true, type: 'error', message: `${res.statusText}: ${res.data.error}`})
            }
        });
    }

    function handleDelete(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        deleteItem('vehicles', currId, source.token).then(res => {
            if (res.status === 200) {
                setItems(prev => {
                    const newItems = new Map(prev);
                    newItems.delete(parseInt(currId));
                    return newItems;
                });
            } else {
                setAlert({alert: true, type: 'error', message: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    function resetStates() {
        setName('');
        setUrl('');
        setSensors(null);
        setBridgeType(null);
        setSelectedItemId(null);
    }

    function onModalClose(action) {
        const data = {name, url, sensors, bridgeType};
        if (action === 'save') {
            if (method === 'POST') {
                postVehicle(data);
            } else if (method === 'PUT') {
                editVehicle(data);
            }
        } else if (action === 'cancel') {
            setModalOpen(false);
            setFormWarning('');
            setMethod('');
        }
        resetStates();
    }

    function onSensorModalClose(action) {
        if (action === 'save') {
            const data = {name, url, sensors, bridgeType};
            if (!isValidJson(sensors)) {
                setFormWarning('Sensor json is not valid. Please check your json again.');
                return;
            }
            if (bridgeType === 'No bridge') delete data.bridgeType;
            if (method === 'POST') {
                postVehicle(data);
            } else if (method === 'PUT') {
                editVehicle(data);
            }
        } else if (action === 'cancel') {
            setSensorModalOpen(false);
            setFormWarning('');
            setMethod('');
        }
        resetStates();
    }

    function postVehicle(data) {
        postItem('vehicles', data, source.token).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newMap = res.data;
                setModalOpen(false);
                setItems(items.set(newMap.id, newMap));
                setFormWarning('');
                setMethod('');
            }
        });
    }

    function editVehicle(data) {
        editItem('vehicles', selectedItemId, data, source.token).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newMap = res.data;
                setModalOpen(false);
                setSensorModalOpen(false);
                setItems(items.set(newMap.id, newMap));
                setFormWarning('');
                setMethod('');
            }
        });
    }

    function openSensorConfig(ev) {
        const vid = parseInt(ev.currentTarget.dataset.vehicleid);
        setSelectedItemId(vid);
        getList('bridge-types').then(res => {
            let bridgeList = res.data;
            bridgeList.unshift({name: 'No bridge'});
            setBridgeTypes(bridgeList);
            const selectedId = vid;
            setMethod('PUT');
            setName(items.get(selectedId).name);
            setUrl(items.get(selectedId).url);
            setSensors(items.get(selectedId).sensors);
            setBridgeType(items.get(selectedId).bridgeType);
            setSensorModalOpen(true);
        });
    }

    function stopDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        stopDownloading('vehicles', currId, source.token).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                setUpdatedVehicle({id: parseInt(currId), progress: 'stopped'});
            }
        });
    }

    function restartDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        restartDownloading('vehicles', currId, source.token).then(res => {
            if (res.status !== 200) {
                setAlert({status: true, type: 'warning', message: res.data.error});
            } else {
                items.get(parseInt(currId)).status = 'Downloading';
                setItems(items);
            }
        });
    }

    function downloadBtn(vehicleStatus, vehicleid) {
        if (vehicleStatus === 'Valid' || updatedVehicle.progress === 100) return;
        return (vehicleStatus === 'Download stopped' || vehicleStatus === 'Invalid' ?
            <FaDownload data-vehicleid={vehicleid} onClick={restartDownloadingMap} /> :
                <FaRegStopCircle data-vehicleid={vehicleid} onClick={stopDownloadingMap} />
        )
    }

    function itemList() {
        const list = [];

        for (const [i, vehicle] of items) {
            let statusText = vehicle.status;
            if (updatedVehicle && vehicle.id === updatedVehicle.id) {
                if (updatedVehicle.status) {
                    statusText = updatedVehicle.status;
                } else if (vehicle.status === 'Downloading') {
                    statusText = `${vehicle.status} ${updatedVehicle.progress}`;
                    if (updatedVehicle.progress !== 'stopped') statusText += '%'
                }
            }
            list.push(
                <div key={`${vehicle}-${i}`} className={appCss.cardItem} data-vehicleid={i}>
                    <div className={appCss.cardName}>{vehicle.name}</div>
                    <div className={appCss.cardUrl}>{vehicle.url}</div>
                    <p className={appCss.cardBottom}>
                        <span className={classNames(appCss.statusDot, appCss[statusText.toLowerCase()])} />
                        <span>{statusText}{statusText == 'Invalid' && ': ' + vehicle.error}</span>
                        {downloadBtn(statusText, vehicle.id)}
                    </p>
                    <FaWrench className={appCss.cardSetting} data-vehicleid={vehicle.id} onClick={openSensorConfig} />
                    <FaPen className={appCss.cardEdit} data-vehicleid={vehicle.id} onClick={openEdit} />
                    <FaRegWindowClose className={appCss.cardDelete} data-vehicleid={vehicle.id} onClick={handleDelete} />
                </div>
            )
        }
        return list;
    }

    return (
        <div className={appCss.pageContainer}>
            {
                alert.status &&
                <Alert type={alert.type} msg={alert.message}>
                    <IoIosClose onClick={alertHide} />
                </Alert>
            }
            <PageHeader title='Vehicles'>
                <button className={appCss.primaryButton} onClick={openAddMewModal}>Add new</button>
            </PageHeader>
            <div className={appCss.cardItemContainer}>
                {isLoading && <p>Loading...</p>}
                {items && itemList()}
            </div>
            {   modalOpen &&
                <FormModal onModalClose={onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Vehicle'}>
                    <h4 className={appCss.inputLabel}>
                        Vehicle Name
                    </h4>
                    <input
                        name="name"
                        type="text"
                        defaultValue={name}
                        placeholder="name"
                        onChange={changeName} />
                    <h4 className={appCss.inputLabel}>
                        Vehicle URL
                    </h4>
                    <p className={appCss.inputDescription}>
                        Enter local or remote URL to pre-built model for adding a new vehicle.
                    </p>
                    <input
                        name="url"
                        type="url"
                        defaultValue={url}
                        placeholder="http://..."
                        onChange={changeUrl} />
                    <span className={appCss.formWarning}>{formWarning}</span>
                </FormModal>
            }
            {   sensorModalOpen &&
                <FormModal onModalClose={onSensorModalClose} title={`${name} Configuration`}>
                    <h4 className={appCss.inputLabel}>
                        Bridge Type
                    </h4>
                    <SingleSelect
                        placeholder='select a Bridge type'
                        defaultValue={bridgeType || 'No bridge'}
                        onChange={changeBridgeType}
                        options={bridgeTypes}
                        label="name"
                        value="name"
                    />
                    <h4 className={appCss.inputLabel}>
                        Sensors
                    </h4>
                    <p className={appCss.inputDescription}>
                        Requires json format
                    </p>
                    <textarea
                        name="sensors"
                        type="text"
                        defaultValue={sensors}
                        onChange={changeSensors} />
                    <span className={appCss.formWarning}>{formWarning}</span>
                </FormModal>
            }
        </div>)
}

export default VehicleManager;