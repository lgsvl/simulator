import React, {useState, useEffect} from 'react'
import {Column, Cell} from '@enact/ui/Layout';
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import SimulationsTable from '../SimulationsTable/SimulationsTable';
import SimulationPlayer from '../Player/Player';
import FormGeneral from './FormGeneral';
import FormMapVehicles from './FormMapVehicles';
import FormTraffic from './FormTraffic';
import FormWeather from './FormWeather';
import {IoIosClose} from "react-icons/io";
import css from './SimulationManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'
import axios from 'axios';
import classnames from 'classnames';
import { SimulationContext } from "../../App/SimulationContext";


const simData = {
    name: null,
    cluster: 0,
    apiOnly: false,
    headless: false,
    map: null,
    vehicles: [],
    interactive: false,
    timeOfDay: null,
    rain: null,
    fog: null,
    wetness: null,
    cloudiness: null,
    useTraffic: false,
    usePedestrians: false,
    useBicyclists: null,
    seed: null
};

const blockingAction = (status) => ['Running', 'Starting', 'Stopping'].includes(status);

function SimulationManager() {
    const [simulation, setSimulation] = useState();
    const [simulations, setSimulations] = useState();
    const [selectedSimulation, setSelectedSimulation] = useState();
    const [selectedTab, setSelectedTab] = useState(0);
    const [method, setMethod] = useState();
    const [formWarning, setFormWarning] = useState();
    const [modalOpen, setModalOpen] = useState();
    const [alert, setAlert] = useState({status: false});
    const [isLoading, setIsLoading] = useState(true);

    let source = axios.CancelToken.source();
    let unmounted;
    useEffect(() => {
        unmounted = false
        const fetchData = async () => {
            setIsLoading(true);
            const result = await getList('simulations', source.token);
            if (result.status === 200) {
                if (!unmounted) {
                    setSimulations(new Map(result.data.map(d => [d.id, d])))
                    setIsLoading(false);
                };
            } else {
                let alertMsg;
                if (!unmounted) {
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
            source.cancel('Cancelling in cleanup.')
        };
    }, []);

    function openAddMewModal() {
        setSimulation(simData);
        setModalOpen(true);
        setMethod('POST');
    }

    function openEdit(id) {
        getItem('simulations', id, source.token).then(res => {
            if (unmounted) return;
            if (res.status === 200) {
                setSimulation(res.data);
                setModalOpen(true);
                setMethod('PUT');
            } else {
                setAlert({status: true, type: 'error', message: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    function handleDelete(id) {
        const deselectSimulation = id === selectedSimulation;
        deleteItem('simulations', id).then(res => {
            if (unmounted) return;
            if (res.status === 200) {
                setModalOpen(false);
                setSelectedSimulation(prev => deselectSimulation ? null : prev);
                setSimulations(prev => {
                    prev.delete(id);
                    const newList = new Map(prev);
                    return newList;
                });
            } else {
                setSelectedSimulation(prev => deselectSimulation ? null : prev);
                setAlert({status: true, type: 'error', message: `${res.data.error}`});
            }
        });
    }

    function postSimulation(data) {
        postItem('simulations', data).then(res => {
            if (unmounted) return;
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newSimulation = res.data;
                setModalOpen(false);
                setFormWarning('');
                setMethod(null);
                const newList = new Map(simulations);
                newList.set(newSimulation.id, newSimulation);
                setSimulations(newList);
            }
            resetStates();
        });
    }

    function editSimulation(data) {
        editItem('simulations', data.id, data).then(res => {
            if (unmounted) return;
            if (res.status !== 200) {
                setFormWarning(res.data.error);
            } else {
                const newSimulation = res.data;
                setModalOpen(false);
                setFormWarning('');
                setMethod(null);
                const newList = new Map(simulations);
                newList.set(newSimulation.id, newSimulation);
                setSimulations(newList);
            }
            resetStates();
        });
    }
    function resetStates() {
        setSelectedTab(0);
    }

    function onModalClose(action) {
        if (action === 'save') {
            if (method === 'POST') {
                delete simulation.id;
                postSimulation(simulation);
            } else if (method === 'PUT') {
                editSimulation(simulation);
            }
        } else if (action === 'cancel') {
            setModalOpen(false);
            setFormWarning('');
            setMethod(null);
            resetStates();
        }
    }

    function selectSimulation (id) {
        setSelectedSimulation(prev => prev === id ? null : id);
    }

    function startSimulation () {
        axios.post(`/simulations/${selectedSimulation}/start`, source.token).catch(err => {
            if (unmounted) return;
            if (err.response && 'data' in err.response) {
                setAlert({status: true, alertType: 'error', alertMsg: err.response.data.error});
            }
        });
    }

    function stopSimulation () {
        axios.post(`/simulations/${selectedSimulation}/stop`, source.token).catch(err => {
            if (unmounted) return;
            if (err.response && 'data' in err.response) {
                setAlert({status: true, alertType: 'error', alertMsg: err.response.data.error});
            }

        });
    }

    function alertHide () {
        setAlert({status: false});
    }

    function simInProgress() {
        for (const [i, sim] of simulations) {
            if (blockingAction(sim.status)) return i;
        }
        return null;
    }

    function changeFormTab(ev) {
        setSelectedTab(parseInt(ev.target.dataset.formtabidx));
    }

    return (
        <Column className={css.simulationManager}>
        {
            alert.status &&
            <Alert type={alert.alertType} msg={alert.alertMsg}>
                <IoIosClose onClick={alertHide} />
            </Alert>
        }
        <Cell shrink>
            <PageHeader title='Simulations'>
                <button className={appCss.primaryButton} onClick={openAddMewModal}>Add new</button>
            </PageHeader>
        </Cell>
        <SimulationContext.Consumer>
        {({simulationEvents}) => {
            if (simulationEvents && simulationEvents.data) {
                const data = JSON.parse(simulationEvents.data);
                if (simulations && simulations.get(data.id).status !== data.status) {
                    simulations.set(data.id, {...data, status: data.status})
                }
            }
            return <React.Fragment>
                <Cell>
                    {!isLoading &&
                        <SimulationsTable
                            simulations={simulations}
                            selected={selectedSimulation}
                            selectSimulation={selectSimulation}
                            openEdit={openEdit}
                            handleDelete={handleDelete}
                        />
                    }
                </Cell>
                { selectedSimulation &&
                    <Cell shrink>
                        <SimulationPlayer
                            open={!!selectSimulation}
                            simulation={simulations.get(selectedSimulation)}
                            title={simulations.get(selectedSimulation).name}
                            status={simulations.get(selectedSimulation).status}
                            handlePlay={startSimulation}
                            handlePause={stopSimulation}
                            simInProgress={simInProgress()}
                        />
                    </Cell>
                }
                </React.Fragment>
                }
            }
        </SimulationContext.Consumer>
        { modalOpen &&
            <FormModal className={css.large} onModalClose={onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Simulation'}>
                <Column style={{width: '600px', height: '500px', overflowY: 'scroll'}}>
                    <Cell shrink>
                        <button
                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 0})}
                            onClick={changeFormTab}
                            data-formtabidx={0}>General</button>
                        <button
                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 1})}
                            onClick={changeFormTab}
                            data-formtabidx={1}>Map & Vehicles</button>
                        <button
                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 2})}
                            onClick={changeFormTab}
                            data-formtabidx={2}>Traffic</button>
                        <button
                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 3})}
                            onClick={changeFormTab}
                            data-formtabidx={3}>Weather</button>
                    </Cell>
                    <Cell style={{padding: '10px'}}>
                    <SimulationContext.Provider value={[simulation, setSimulation]}>
                        {selectedTab === 0 && <FormGeneral />}
                        {selectedTab === 1 && <FormMapVehicles />}
                        {selectedTab === 2 && <FormTraffic />}
                        {selectedTab === 3 && <FormWeather />}
                    </SimulationContext.Provider>
                    </Cell>
                </Column>
                <span className={appCss.formWarning}>{formWarning}</span>
            </FormModal>
        }
    </Column>)
};

export default SimulationManager;
