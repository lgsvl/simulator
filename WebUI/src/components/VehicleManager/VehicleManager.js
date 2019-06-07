import React, {useState, useEffect, useContext} from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaWrench, FaPen, FaRegWindowClose, FaRegStopCircle, FaDownload } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem, stopDownloading, restartDownloading} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import classNames from 'classnames';

function VehicleManager() {
    const [items, setItems] = useState();
    const [modalOpen, setModalOpen] = useState();
    const [name, setName] = useState();
    const [url, setUrl] = useState();
    const [id, setId] = useState();
    const [method, setMethod] = useState();
    const [formWarning, setFormWarning] = useState();
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext).mapDownloadEvents;
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
        getItem('vehicles', ev.currentTarget.dataset.mapid).then(res => {
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
        const currId = ev.currentTarget.dataset.mapid;
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
        const data = {name, url};
        if (action === 'save') {
            if (method === 'POST') {
                postMap(data);
            } else if (method === 'PUT') {
                editMap(id, data);
            }
        } else if (action === 'cancel') {
            setModalOpen(false);
            setFormWarning('');
            setMethod('');
        }
    }

    function postMap(data) {
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

    function editMap(currId, data) {
        editItem('vehicles', currId, data).then(res => {
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

    function openSensorConfig() {

    }

    function stopDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.mapid;
        stopDownloading('vehicles', currId).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                items.get(parseInt(currId)).status = 'Download stopped';
                setItems(items);
                console.log(items.get(parseInt(currId)).status)
            }
        });
    }

    function restartDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.mapid;
        restartDownloading('vehicles', currId).then(res => {
            if (res.status !== 200) {
                setAlert({status: true, type: 'warning', message: res.data.error});
            } else {
                items.get(parseInt(currId)).status = 'Downloading';
                setItems(items);
            }
        });
    }

    function downloadBtn(mapStatus, mapid) {
        return (mapStatus === 'Download stopped' || mapStatus === 'Invalid' ?
            <FaDownload data-mapid={mapid} onClick={restartDownloadingMap} /> :
                <FaRegStopCircle data-mapid={mapid} onClick={stopDownloadingMap} />
        )
    }

    function itemList() {
        let updatedVehicle;
        if (context) updatedVehicle = JSON.parse(context.data);
        const list = [];
        for (const [i, item] of items) {
            let itemStaus = item.status;
            if (updatedVehicle) console.log(itemStaus,updatedVehicle.id === i, updatedVehicle.progress !== 100, updatedVehicle.id, updatedVehicle.progress)
            if (updatedVehicle && updatedVehicle.id === i) {
                itemStaus = updatedVehicle.progress !== 100 ? `Downloading: ${updatedVehicle.progress}%` : 'Downloaded';
            }
            list.push(
                <div key={`${item}-${i}`} className={appCss.cardItem} data-itemid={i}>
                    <div className={appCss.cardName}>{item.name}</div>
                    <div className={appCss.cardUrl}>{item.url}</div>
                    <p className={appCss.cardBottom}>
                        <span className={classNames(appCss.statusDot, appCss[itemStaus.toLowerCase()])} />
                        <span>{itemStaus}</span>
                        {downloadBtn(itemStaus, item.id)}
                    </p>
                    <FaPen className={appCss.cardEdit} data-itemid={item.id} onClick={openEdit} />
                    <FaRegWindowClose className={appCss.cardDelete} data-itemid={item.id} onClick={handleDelete} />
                    <FaWrench className={appCss.cardSetting} data-itemid={item.id} onClick={openSensorConfig}/>
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
                <FormModal onModalClose={onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Map'}>
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
        </div>)
}

export default VehicleManager;