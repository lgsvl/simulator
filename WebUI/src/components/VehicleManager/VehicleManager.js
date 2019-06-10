import React, {useState, useEffect, useContext} from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaWrench, FaPen, FaRegWindowClose, FaRegStopCircle, FaDownload } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem, patchItem, stopDownloading, restartDownloading} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';

function VehicleManager() {
    const [items, setItems] = useState();
    const [modalOpen, setModalOpen] = useState();
    const [sensorModalOpen, setSensorModalOpen] = useState();
    const [name, setName] = useState();
    const [url, setUrl] = useState();
    const [id, setId] = useState();
    const [sensors, setSensors] = useState();
    const [method, setMethod] = useState();
    const [formWarning, setFormWarning] = useState();
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext);
    const [updatedVehicle, setUpdatedVehicle] = useState({progress: null, id: null});
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        const fetchData = async () => {
            setAlert({status: false});
            setIsLoading(true);
            const result = await getList('vehicles');
            if (result.status === 200) {
                const itemsData = new Map(result.data.map(d => [d.id, d]));
                setItems(itemsData);
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

    useEffect(() => {
        if (context && context.vehicleDownloadEvents) {
            let contextData = JSON.parse(context.vehicleDownloadEvents.data);
            switch (context.vehicleDownloadEvents.type) {
                case 'VehicleDownloadComplete':
                    setUpdatedVehicle({id: contextData.id, status: contextData.status});
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
        setModalOpen(true);
        setName('');
        setUrl('');
        setId(null);
        setMethod('POST');
    }

    function openEdit(ev) {
        setId(ev.currentTarget.dataset.vehicleid);
        getItem('vehicles', ev.currentTarget.dataset.vehicleid).then(res => {
            if (res.status === 200) {
                setModalOpen(true);
                setId(res.data.id);
                setName(res.data.name);
                setUrl(res.data.url);
                setMethod('PUT');
            } else {
                setAlert({status: true, type: 'error', message: `${res.statusText}: ${res.data.error}`})
            }
        });
    }

    function handleDelete(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        deleteItem('vehicles', currId).then(res => {
            if (res.status === 200) {
                items.delete(parseInt(currId));
                setModalOpen(false);
                setItems(items);
            } else {
                setAlert({alert: true, type: 'error', message: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    function onModalClose(action) {
        const data = {name, url, sensors};
        if (action === 'save') {
            if (method === 'POST') {
                postVehicle(data);
            } else if (method === 'PUT') {
                editVehicle(data);
            }
        } else if (action === 'cancel') {
            setModalOpen(false);
            setSensorModalOpen(false);
            setFormWarning('');
            setMethod('');
        }
    }

    function postVehicle(data) {
        postItem('vehicles', data).then(res => {
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
        editItem('vehicles', id, data).then(res => {
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
        const selectedId = parseInt(ev.currentTarget.dataset.vehicleid);
        const selectedItem = items.get(selectedId);
        setSensorModalOpen(true);
        setMethod('PUT');
        setId(selectedId);
        setName(selectedItem.name);
        setUrl(selectedItem.url);
        setSensors(selectedItem.sensors);
    }

    function stopDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        stopDownloading('vehicles', currId).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                setUpdatedVehicle({id: parseInt(currId), progress: 'stopped'});
            }
        });
    }

    function restartDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.vehicleid;
        restartDownloading('vehicles', currId).then(res => {
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
                        <span>{statusText}</span>
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
        <div className={appCss.cardManager}>
            {
                alert.status &&
                <Alert type={alert.type} msg={alert.message}>
                    <IoIosClose onClick={alertHide} />
                </Alert>
            }
            <PageHeader title='Vehicles'>
                <button className={appCss.primaryButton} onClick={openAddMewModal}>Add new</button>
            </PageHeader>
            {!isLoading && <div className={appCss.cardItemContainer}>
                {items && itemList()}
            </div>}
            {   modalOpen &&
                <FormModal onModalClose={onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Vehicle'}>
                    <input
                        name="name"
                        type="text"
                        defaultValue={name}
                        placeholder="name"
                        onChange={e => setName(e.target.value)} />
                    <input
                        name="url"
                        type="url"
                        defaultValue={url}
                        placeholder="url"
                        onChange={e => setUrl(e.target.value)} />
                    <span className={appCss.formWarning}>{formWarning}</span>
                </FormModal>
            }
            {   sensorModalOpen &&
                <FormModal onModalClose={onModalClose} title='Sensor configuration'>
                    <textarea
                        name="sensors"
                        type="text"
                        defaultValue={sensors}
                        onChange={e => setSensors(e.target.value)} />
                    <span className={appCss.formWarning}>{formWarning}</span>
                </FormModal>
            }
        </div>)
}

export default VehicleManager;