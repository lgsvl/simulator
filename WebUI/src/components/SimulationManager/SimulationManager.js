import React from 'react'
import {Column, Cell} from '@enact/ui/Layout';
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Checkbox from '../Checkbox/Checkbox';
import Alert from '../Alert/Alert';
import SingleSelect from '../Select/SingleSelect';
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
import { SimulationConsumer } from "../../App/SimulationContext";

const simData = {
    name: null,
    map: null,
    vehicles: [],
    apiOnly: false,
    interactive: false,
    offScreen: false,
    cluster: 0,
    timeOfDay: null,
    rain: null,
    fog: null,
    wetness: null,
    cloudiness: null,
    useTraffic: false,
    usePedestrians: false,
    hasSeed: null,
    seed: Math.floor(Math.random() * Number.MAX_SAFE_INTEGER) + 1,
    selectedTab: 0
};
const blockingAction = (status) => ['Running', 'Starting', 'Stopping'].includes(status);

class SimulationManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            simulations: [],
            ...Object.assign({}, simData),
            id: ''
        }
    }

    componentDidMount() {
        getList('simulations').then(res => {
            if (res && res.status === 200) {
                let runningSimulation;
                const simulations = new Map(res.data.map(d => [d.id, d]));
                this.setState({simulations, runningSimulation});
            } else {
                let alertMsg;
                if (res.name === "Error") {
                    alertMsg = res.message;
                } else {
                    alertMsg = `${res.statusText}: ${res.data.error}`;
                }
                this.setState({alert: true, alertType: 'error', alertMsg});
            }
        });
    }

    getSelectOptions() {
        getList('maps').then(res => {
            if (res.status === 200) {
                this.setState({mapList: res.data, map: res.data[0].id});
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
        getList('vehicles').then(res => {
            if (res.status === 200) {
                this.setState({vehicleList: res.data});
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
        getList('clusters').then(res => {
            if (res.status === 200) {
                this.setState({clusterList: res.data, cluster: res.data[0].id});
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
        getList('bridge-types').then(res => {
            if (res.status === 200) {
                this.setState({bridgeList: res.data, bridge: res.data[0].id});
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    openAddMewModal = () => {
        this.getSelectOptions();
        this.setState({modalOpen: true, method: 'POST', ...Object.assign({}, simData)});
    }

    openEdit = (id) => {
        this.getSelectOptions();
        getItem('simulations', id).then(res => {
            if (res.status === 200) {
                const {name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, weather, useTraffic, usePedestrians, seed} = res.data;
                const {rain, fog, wetness, cloudiness} = weather;
                this.setState({
                    modalOpen: true,
                    id,
                    name,
                    map,
                    vehicles,
                    apiOnly,
                    interactive,
                    offScreen,
                    cluster,
                    timeOfDay,
                    rain,
                    fog,
                    wetness,
                    cloudiness,
                    useTraffic,
                    usePedestrians,
                    seed,
                    method: 'PUT'
                });
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    handleDelete = (id) => {
        const deselectSimulation = id === this.state.selectedSimulation;
        deleteItem('simulations', id).then(res => {
            if (res.status === 200) {
                this.setState(prevState => {
                    prevState.simulations.delete(parseInt(id));
                    return {
                        modalOpen: false,
                        data: prevState.simulations,
                        selectedSimulation: deselectSimulation ? null : prevState.selectedSimulation
                    }
                });
            } else {
                this.setState(prevState => {
                    return {
                        alert: true,
                        alertType: 'error',
                        alertMsg: `${res.statusText}: ${res.data.error}`,
                        selectedSimulation: deselectSimulation ? null : prevState.selectedSimulation
                    }
                });
            }
        });
    }

    handleInputChange = (ev) => {
        const target = ev.target;
        let value;
        if (target.type === 'checkbox') value = target.checked;
        else if (target.type === 'text' || target.type === 'number') value = target.value;
        this.setState({[target.name]: value});
    }

    handleSelectInputChange = ev => {
        const target = ev.target;
        this.setState({[target.dataset.for]: parseInt(target.value)});
    }

    handleMultiSelectInputChange = ev => {
        const target = ev.target;
        this.setState({[target.dataset.for]: [...target.options].filter(o => o.selected).map(o => parseInt(o.value))});
    }

    postSimulation = (data) => {
        postItem('simulations', data).then(res => {
            if (res.status !== 200) {
                this.setState({formWarning: res.data.error});
            } else {
                const newSimulation = res.data;
                this.setState(prevState => ({modalOpen: false, data: prevState.simulations.set(newSimulation.id, newSimulation), formWarning: '', method: null}));
            }
        });
    }

    editSimulation = (data) => {
        editItem('simulations', data.id, data).then(res => {
            if (res.status !== 200) {
                this.setState({formWarning: res.data.error});
            } else {
                const newSimulation = res.data;
                this.setState(prevState => {
                    prevState.simulations.set(newSimulation.id, newSimulation);
                    return {modalOpen: false, maps: prevState.simulations, formWarning: '', method: null};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {id, name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay,
            rain, fog, wetness, cloudiness, useTraffic, usePedestrians, hasSeed, seed} = this.state;
        const data = {
            id,
            name,
            map,
            vehicles,
            apiOnly,
            interactive,
            offScreen,
            cluster,
            timeOfDay,
            weather: {
                rain,
                fog,
                wetness,
                cloudiness
            },
            useTraffic,
            usePedestrians
        }
        if (hasSeed) data.seed = seed;
        if (action === 'save') {
            if (this.state.method === 'POST') {
                delete data.id;
                data.timeOfDay = timeOfDay || new Date();
                this.postSimulation(data);
            } else if (this.state.method === 'PUT') {
                this.editSimulation(data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false, formWarning: '', method: null});
        }
    }

    selectSimulation = (id) => {
        this.setState(prevState => {
            if (prevState.selectedSimulation === id) {
                return {selectedSimulation: null};
            } else {
                return {selectedSimulation: id};
            }
        });
    }

    startSimulation = () => {
        const id = this.state.selectedSimulation;
        axios.post(`/simulations/${id}/start`).catch(err => {
            if (err.response && 'data' in err.response) {
                this.setState({alert: true, alertType: 'error', alertMsg: err.response.data.error});
            }
        });
    }

    stopSimulation = () => {
        const id = this.state.selectedSimulation;
        axios.post(`/simulations/${id}/stop`).catch(err => {
            if (err.response && 'data' in err.response) {
                this.setState({alert: true, alertType: 'error', alertMsg: err.response.data.error});
            }

        });
    }

    alertHide = () => {
        this.setState({alert: false});
    }

    simInProgress = (simulations) => {
        for (const [i, simulation] of simulations) {
            if (blockingAction(simulation.status)) return i;
        }
        return null;
    }

    changeFormTab = ev => {
        this.setState({selectedTab: parseInt(ev.target.dataset.formtabidx)});
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, simulations, mapList, clusterList, vehicleList, method, formWarning, selectedSimulation,
            name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, rain, fog, wetness, cloudiness,
            enableNpc, enablePedestrian, hasSeed, seed, selectedTab,
            alert, alertType, alertMsg} = this.state;

            return (
            <SimulationConsumer>
                {({simulationEvents}) => {
                    this.events = simulationEvents;
                    if (simulationEvents && simulationEvents.data) {
                        const data = JSON.parse(simulationEvents.data);
                        if (simulations.get(data.id).status !== data.status) {
                            simulations.set(data.id, {...data, status: data.status})
                        }
                    }
                    const simInProgress = this.simInProgress(simulations);
                    return <React.Fragment><Column className={css.simulationManager} {...rest}>
                        {
                            alert &&
                            <Alert type={alertType} msg={alertMsg}>
                                <IoIosClose onClick={this.alertHide} />
                            </Alert>
                        }
                        <Cell shrink>
                            <PageHeader title='Simulations'>
                                <button className={appCss.primaryButton} onClick={this.openAddMewModal}>Add new</button>
                            </PageHeader>
                        </Cell>
                        <Cell>
                            {simulations ?
                                <SimulationsTable
                                    simulations={simulations}
                                    selected={selectedSimulation}
                                    selectSimulation={this.selectSimulation}
                                    openEdit={this.openEdit}
                                    handleDelete={this.handleDelete}
                                />
                                :
                                <p>Please add a new Simulation.</p>
                            }
                        </Cell>
                        { selectedSimulation &&
                            <Cell shrink>
                                <SimulationPlayer
                                    open={!!this.selectSimulation}
                                    simulation={simulations.get(selectedSimulation)}
                                    title={simulations.get(selectedSimulation).name}
                                    status={simulations.get(selectedSimulation).status}
                                    handlePlay={this.startSimulation}
                                    handlePause={this.stopSimulation}
                                    simInProgress={simInProgress}
                                />
                            </Cell>
                        }
                        { modalOpen &&
                            <FormModal className={css.large} onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Simulation'}>
                                <Column style={{width: '600px', height: '450px', overflowY: 'scroll'}}>
                                    <Cell shrink>
                                        <button
                                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 0})}
                                            onClick={this.changeFormTab}
                                            data-formtabidx={0}>General</button>
                                        <button
                                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 1})}
                                            onClick={this.changeFormTab}
                                            data-formtabidx={1}>Map & Vehicles</button>
                                        <button
                                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 2})}
                                            onClick={this.changeFormTab}
                                            data-formtabidx={2}>Tracffic</button>
                                        <button
                                            className={classnames(css.tabButton, {[css.selected]: selectedTab === 3})}
                                            onClick={this.changeFormTab}
                                            data-formtabidx={3}>Weather</button>
                                    </Cell>
                                    <Cell style={{padding: '10px'}}>
                                        {selectedTab === 0 && <FormGeneral />}
                                        {selectedTab === 1 && <FormMapVehicles apiOnly={apiOnly} offScreen={offScreen} />}
                                        {selectedTab === 2 && <FormTraffic seed={seed} hasSeed={hasSeed} />}
                                        {selectedTab === 3 && <FormWeather timeOfDay={timeOfDay} cloudiness={cloudiness} rain={rain} wetness={wetness} fog={fog} />}
                                    </Cell>
                                </Column>
                                <span className={appCss.formWarning}>{formWarning}</span>
                            </FormModal>
                        }
                    </Column>
                    </React.Fragment>
                }
            }
            </SimulationConsumer>
        )
    }
};

export default SimulationManager;
