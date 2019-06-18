import React, {useState, useEffect, useContext, useCallback} from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaRegWindowClose, FaRegStopCircle, FaDownload } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import classNames from 'classnames';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem, stopDownloading, restartDownloading} from '../../APIs.js'
import { SimulationContext } from "../../App/SimulationContext";
import axios from 'axios';

function MapManager() {
    const [maps, setMaps] = useState(new Map());
    const [modalOpen, setModalOpen] = useState();
    const [name, setName] = useState();
    const [url, setUrl] = useState();
    const [id, setId] = useState();
    const [method, setMethod] = useState();
    const [formWarning, setFormWarning] = useState();
    const [alert, setAlert] = useState({status: false});
    const context = useContext(SimulationContext);
    const [updatedMap, setUpdatedMap] = useState();
    const [isLoading, setIsLoading] = useState(false);

    const changeName = useCallback(ev => setName(ev.target.value));
    const changeUrl = useCallback(ev => setUrl(ev.target.value));
    let source = axios.CancelToken.source();

    useEffect(() => { 
        let unmounted = false;
        const fetchData = async () => {
            setIsLoading(true);
            const result = await getList('maps', source.token);
            if (result.status === 200) {
                if (!unmounted) {
                    const mapsData = new Map(result.data.map(d => [d.id, d]));
                    setMaps(mapsData);
                    setIsLoading(false);
                }
            } else {
                if (!unmounted) {
                    let alertMsg;
                    if (result.name === "Error") {
                        alertMsg = result.message;
                    } else {
                        alertMsg = `${result.statusText}: ${result.data.error}`;
                    }
                    setAlert({status: true, type: 'error', message: alertMsg});
                }
            }
        };
        fetchData();
        return () => {
            unmounted = true;
            source.cancel('Cancelling in cleanup.');
        };
    }, []);

    useEffect(() => {
        if (context && context.mapDownloadEvents) {
            let contextData = JSON.parse(context.mapDownloadEvents.data);
            switch (context.mapDownloadEvents.type) {
                case 'MapDownloadComplete':
                    setUpdatedMap({id: contextData.id, status: contextData.status});
                    break;
                case 'MapDownload':
                    setUpdatedMap(contextData);
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
        getItem('maps', ev.currentTarget.dataset.mapid, source.token).then(res => {
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
        deleteItem('maps', currId, source.token).then(res => {
            if (res.status === 200) {
                setMaps(prev => {
                    const newItems = new Map(prev);
                    newItems.delete(parseInt(currId));
                    return newItems;
                });
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
        postItem('maps', data, source.token).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newMap = res.data;
                setModalOpen(false);
                setMaps(maps.set(newMap.id, newMap));
                setFormWarning('');
                setMethod('');
            }
        });
    }

    function editMap(currId, data) {
        editItem('maps', currId, data, source.token).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newMap = res.data;
                setModalOpen(false);
                setMaps(maps.set(newMap.id, newMap));
                setFormWarning('');
                setMethod('');
            }
        });
    }

    function stopDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.mapid;
        stopDownloading('maps', currId).then(res => {
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                setUpdatedMap({id: parseInt(currId), progress: 'stopped'});
            }
        });
    }

    function restartDownloadingMap(ev) {
        const currId = ev.currentTarget.dataset.mapid;
        restartDownloading('maps', currId, source.token).then(res => {
            if (res.status !== 200) {
                setAlert({status: true, type: 'warning', message: res.data.error});
            } else {
                maps.get(parseInt(currId)).status = 'Downloading';
                setMaps(maps);
            }
        });
    }

    function downloadBtn(mapStatus, mapid) {
        if (mapStatus === 'Valid' || (updatedMap && updatedMap.progress === 100)) return;
        return ((updatedMap && updatedMap.progress === 'stopped') || mapStatus === 'Invalid' ?
            <FaDownload data-mapid={mapid} onClick={restartDownloadingMap} /> :
                <FaRegStopCircle data-mapid={mapid} onClick={stopDownloadingMap} />
        )
    }

    function itemList() {
        const list = [];
        for (const [i, map] of maps) {
            let statusText = map.status;
            if (updatedMap && map.id === updatedMap.id) {
                if (updatedMap.status) {
                    statusText = updatedMap.status;
                } else if (map.status === 'Downloading') {
                    statusText = `${map.status} ${updatedMap.progress}`;
                    if (updatedMap.progress !== 'stopped') statusText += '%'
                }
            }
            list.push(
                <div key={`${map}-${i}`} className={appCss.cardItem} data-mapid={i}>
                    <div className={appCss.cardName}>{map.name}</div>
                    <div className={appCss.cardUrl}>{map.url}</div>
                    <p className={appCss.cardBottom}>
                        <span className={classNames(appCss.statusDot, appCss[statusText.toLowerCase()])} />
                        <span>{statusText}</span>
                        {downloadBtn(statusText, map.id)}
                    </p>
                    <FaRegEdit className={appCss.cardEdit} data-mapid={map.id} onClick={openEdit} />
                    <FaRegWindowClose className={appCss.cardDelete} data-mapid={map.id} onClick={handleDelete} />
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
            <PageHeader title='Maps'>
                <button className={appCss.primaryButton} onClick={openAddMewModal}>Add new</button>
            </PageHeader>
            <div className={appCss.cardItemContainer}>
                {isLoading && <div>Loading...</div>}
                {maps && itemList()}
            </div>
            {   modalOpen &&
                <FormModal onModalClose={onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Map'}>
                    <input
                        name="name"
                        type="text"
                        defaultValue={name}
                        placeholder="name"
                        onChange={changeName} />
                    <input
                        name="url"
                        type="url"
                        defaultValue={url}
                        placeholder="url"
                        onChange={changeUrl} />
                    <span className={appCss.formWarning}>{formWarning}</span>
                </FormModal>
            }
        </div>)
}

export default MapManager;