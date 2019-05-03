import React from 'react'
import {Column, Cell} from '@enact/ui/Layout';
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Checkbox from '../Checkbox/Checkbox';
import Alert from '../Alert/Alert';
import SingleSelect from '../Select/SingleSelect';
import MultiSelect from '../Select/MultiSelect';
import SimulationsTable from '../SimulationsTable/SimulationsTable';
import SimulationPlayer from '../Player/Player';
import {IoIosClose} from "react-icons/io";
import css from './SimulationManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'
import axios from 'axios';
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
    enableNpc: false,
    enablePedestrian: false
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
    }

    openAddMewModal = () => {
        this.getSelectOptions();
        this.setState({modalOpen: true, method: 'POST', ...Object.assign({}, simData)});
    }

    openEdit = (id) => {
        this.getSelectOptions();
        getItem('simulations', id).then(res => {
            if (res.status === 200) {
                const {name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, weather, enableNpc, enablePedestrian} = res.data;
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
                    enableNpc,
                    enablePedestrian,
                    method: 'PUT'
                });
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    handleDelete = (id) => {
        deleteItem('simulations', id).then(res => {
            if (res.status === 200) {
                this.setState(prevState => {
                    prevState.simulations.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.simulations}
                });
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
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
        const {id, name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, rain, fog, wetness, cloudiness, enableNpc, enablePedestrian} = this.state;
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
            enableNpc,
            enablePedestrian
        }
        if (action === 'save') {
            if (this.state.method === 'POST') {
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

    render() {
        const {...rest} = this.props;
        const {modalOpen, simulations, mapList, clusterList, vehicleList, method, formWarning, selectedSimulation,
            name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, rain, fog, wetness, cloudiness, enableNpc, enablePedestrian, 
            alert, alertType, alertMsg} = this.state;

            return (
            <SimulationConsumer>
                {({events}) => {
                    this.events = events;
                    if (events && events.data) {
                        const data = JSON.parse(events.data);
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
                            <PageHeader title='Simulation Manager'>
                                <button onClick={this.openAddMewModal}>Add new</button>
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
                            <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Simulation'}>
                                <input
                                    required
                                    name="name"
                                    type="text"
                                    defaultValue={name}
                                    placeholder="name"
                                    onChange={this.handleInputChange} />
                                <Checkbox checked={apiOnly} label="API Only" name={'apiOnly'} onChange={this.handleInputChange} disabled={interactive}/>
                                <SingleSelect
                                    data-for='cluster'
                                    placeholder='select a cluster'
                                    defaultValue={cluster}
                                    onChange={this.handleSelectInputChange}
                                    options={clusterList}
                                    label="name"
                                    value="id"
                                />
                                <SingleSelect
                                    data-for='map'
                                    placeholder='select a map'
                                    defaultValue={map}
                                    onChange={this.handleSelectInputChange}
                                    options={mapList}
                                    label="name"
                                    value="id"
                                    disabled={apiOnly}
                                />
                                {vehicleList && <MultiSelect data-for='vehicles' size={vehicleList.length} defaultValue={vehicles} onChange={this.handleMultiSelectInputChange} options={vehicleList} label="name" value="id" disabled={apiOnly} />}
                                <Checkbox checked={interactive} label="Interactive"  name={'interactive'} disabled={apiOnly || offScreen} onChange={this.handleInputChange} />
                                <Checkbox checked={offScreen} label="Off-screen Rendering"  name={'offScreen'} disabled={interactive} onChange={this.handleInputChange} />
                                <br />
                                <label className={appCss.inputLabel}>Time of day</label><br />
                                <input name="weather" type="text" defaultValue={timeOfDay || new Date()} onChange={this.handleInputChange} />
                                <br />
                                <label className={appCss.inputLabel}>Weather</label><br />
                                <label className={appCss.inputLabel}>min: 0, max: 1.0</label>
                                <input type="number" name="cloudiness" defaultValue={cloudiness} onChange={this.handleInputChange} step="0.01" placeholder="cloudiness"/>
                                <input type="number" name="rain" defaultValue={rain} onChange={this.handleInputChange} step="0.01" placeholder="rain"/>
                                <input type="number" name="wetness" defaultValue={wetness} onChange={this.handleInputChange} step="0.01" placeholder="wetness"/>
                                <input type="number" name="fog" defaultValue={fog} onChange={this.handleInputChange} step="0.01" placeholder="fog"/>
                                <Checkbox checked={enableNpc} label="Enable NPC"  name={'enableNpc'} disabled={apiOnly} onChange={this.handleInputChange} />
                                <Checkbox checked={enablePedestrian} label="Enable Pedestrians"  name={'enablePedestrian'} disabled={apiOnly} onChange={this.handleInputChange} />
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
