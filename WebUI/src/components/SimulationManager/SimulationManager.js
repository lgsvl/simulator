import React from 'react'
import {Column, Cell} from '@enact/ui/Layout';
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Checkbox from '../Checkbox/Checkbox';
import Alert from '../Alert/Alert';
import SingleSelect from '../Select/SingleSelect';
import MultiSelect from '../Select/MultiSelect';
import SimulationPlayer from '../Player/Player';
import {FaRegEdit, FaRegWindowClose} from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import css from './SimulationManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'
import axios from 'axios';
import classNames from 'classnames';
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
    cloudiness: null
};
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
            if (res.status === 200) {
                const simulations = new Map(res.data.map(d => [d.id, d]));
                this.setState({simulations});
            } else {
                this.setState({alert: true, alertType: res.name, alertMsg: res.message})
            }
        });
    }

    getSelectOptions() {
        getList('maps').then(res => {
            if (res.data) {
                this.setState({mapList: res.data, map: res.data[0].id});
            } else {
                this.setState({forWarning: res.message})
            }
        });
        getList('vehicles').then(res => {
            if (res.data) {
                this.setState({vehicleList: res.data});
            } else {
                this.setState({forWarning: res.message})
            }
        });
        getList('clusters').then(res => {
            if (res.data) {
                this.setState({clusterList: res.data, cluster: res.data[0].id});
            } else {
                this.setState({forWarning: res.message})
            }
        });
    }

    openAddMewModal = () => {
        this.getSelectOptions();
        console.log(Object.assign({}, simData))
        this.setState({modalOpen: true, method: 'POST', ...Object.assign({}, simData)});
    }

    openEdit = (ev) => {
        this.getSelectOptions();
        const id = ev.currentTarget.dataset.simulationid;
        getItem('simulations', id).then(data => {
            const {name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, weather} = data;
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
                method: 'PUT'
            })
        });
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.simulationid;
        deleteItem('simulations', id).then(() => {
                this.setState(prevState => {
                    prevState.simulations.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.simulations}
                });
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
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newSimulation = res.data;
                if (newSimulation.responseStatus === 'error') {
                    this.setState({formWarning: newSimulation.error});
                } else {
                    this.setState(prevState => ({modalOpen: false, data: prevState.simulations.set(newSimulation.id, newSimulation)}));
                }
            }
        });
    }

    editSimulation = (data) => {
        editItem('simulations', data.id, data).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newSimulation = res.data;
                this.setState(prevState => {
                    prevState.simulations.set(newSimulation.id, newSimulation);
                    return {modalOpen: false, simulations: prevState.simulations};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {id, name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, rain, fog, wetness, cloudiness} = this.state;
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
            }
        }
        if (action === 'save') {
            if (this.state.method === 'POST') {
                this.postSimulation(data);
            } else if (this.state.method === 'PUT') {
                this.editSimulation(data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false, method: null});
        }
    }

    selectSimulation = (events) => (ev) => {
        debugger
        const running = events && events.data.toLowerCase() === 'runnning';
        const {simulations} = this.state;
        const id = parseInt(ev.currentTarget.dataset.simulationid);
        if (running) {
            this.setState({alert: true, alertType: 'warning', alertMsg: `${simulations.get(id).name} is already running.`});
        } else {
            this.setState(prevState => {
                if (prevState.selectedSimulation === id) {
                    return {selectedSimulation: null};
                } else {
                    return {selectedSimulation: id};
                }
            });
        }
    }

    startSimulation = () =>{
        const id = this.state.selectedSimulation;
        axios.post(`http://localhost:8079/simulations/${id}/start`);
    }

    stopSimulation = () =>{
        const id = this.state.selectedSimulation;
        axios.post(`http://localhost:8079/simulations/${id}/stop`);
    }

    alertHide = () => {
        this.setState({alert: false});
    }

    simulationList = (events) => {
        const list = [];
        for (const [i, simulation] of this.state.simulations) {
            const classes = classNames(css.simulationItem, {[css.selected]: this.state.selectedSimulation === i});
            list.push(
                <tr key={`${simulation}-${i}`} className={classes} data-simulationid={i}>
                    <td data-simulationid={i} onClick={this.selectSimulation(events)}>{simulation.name}</td>
                    {/* <td>{simulation.status}</td> */}
                    <td data-simulationid={simulation.id} onClick={this.openEdit}><FaRegEdit /></td>
                    <td data-simulationid={simulation.id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                </tr>
            )
        }
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, simulations, mapList, clusterList, vehicleList, method, running, formWarning, selectedSimulation,
            name, map, vehicles, apiOnly, interactive, offScreen, cluster, timeOfDay, rain, fog, wetness, cloudiness,
            alert, alertType, alertMsg} = this.state;

            return (
            <SimulationConsumer>
                {({events}) => {
                    console.log(events)
                    // if (events && events.data === '')
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
                                <table>
                                    <tbody>{this.simulationList(events)}</tbody>
                                </table>
                                :
                                <p>Please add a new Simulation.</p>
                            }
                        </Cell>
                        { selectedSimulation &&
                            <Cell shrink>
                                <SimulationPlayer
                                    open={!!this.selectSimulation}
                                    title={simulations.get(selectedSimulation).name}
                                    description={events ? events.data : ''}
                                    running={running}
                                    handlePlay={this.startSimulation}
                                    handlePause={this.stopSimulation}
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
