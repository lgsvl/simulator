import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Checkbox from '../Checkbox/Checkbox';
import SingleSelect from '../Select/SingleSelect';
import SimulationPlayer from '../Player/Player';
import { FaRegEdit, FaRegWindowClose, FaPlay } from 'react-icons/fa';
import css from './SimulationManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'
import axios from 'axios';

const simulationValues = {
    name: null,
    map: null,
    vehicles: [],
    apiOnly: false,
    interactive: false,
    offScreen: false,
    cluster: 0,
    timeOfDay: null,
    weather: {
        rain: 0,
        fog: 0,
        wetness: 0,
        cloudiness: 0
    }
}
class SimulationManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            simulations: [],
            data: {
                id: '',
                values: simulationValues
            }
        }
    }

    componentDidMount() {
        getList('simulations').then(data => {
            const simulations = new Map(data.map(d => [d.id, d]));
            this.setState({simulations});
        });
    }

    getSelectOptions() {
        getList('maps').then(data => this.setState({maps: data}));
        getList('vehicles').then(data => this.setState({vehicles: data}));
        getList('clusters').then(data => this.setState({clusters: data}));
    }

    openAddMewModal = () => {
        this.getSelectOptions();
        this.setState({modalOpen: true, data: {values: simulationValues}, method: 'POST'});
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.simulationid;
        getItem('simulations', id).then(data => {
            console.log('openEdit',data)
            this.setState({modalOpen: true, data: {id, values: data}, method: 'PUT'})
        });
        this.getSelectOptions();
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.simulationid;
        deleteItem('simulations', id).then(({responseStatus}) => {
            if (responseStatus === 'success') {
                this.setState(prevState => {
                    prevState.simulations.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.simulations}
                });
            }
        });
    }

    handleInputChange = (ev) => {
        const target = ev.target;
        let value;
        if (target.type === 'checkbox') value = target.checked;
        else if (target.type === 'text') value = target.value;
        console.log(value, target)
        this.setState(prevState => ({
            data: {
                ...prevState.data,
                values: {
                    ...prevState.data.values,
                    [target.name]: value
                }
            }
        }));
    }

    handleSelectInputChange = ev => {
        const target = ev.target;
        console.log(target)
        debugger
    }

    onModalClose = (action) => {
        const {data} = this.state;
        if (action === 'save') {
            if (this.state.method === 'POST') {
                postItem('simulations', data.values).then(newMap => {
                    // console.log(newMap)
                    // debugger
                    // if (newMap.responseStatus === 'success') {
                    //     this.setState(prevState => ({modalOpen: false, data: prevState.simulations.set(newMap.id, newMap)}));
                    // } else if (newMap.responseStatus === 'error') {
                    //     console.log(newMap.error)
                    //     this.setState({warning: newMap.error});
                    // }
                    if (newMap.responseStatus === 'error') {
                        // console.log(newMap.error)
                        this.setState({warning: newMap.error});
                    } else {
                        this.setState(prevState => ({modalOpen: false, data: prevState.simulations.set(newMap.id, newMap)}));
                    }
                })
            } else if (this.state.method === 'PUT') {
                editItem('simulations', data.id, data.values).then(newMap => {
                    this.setState(prevState => {
                        prevState.simulations.set(newMap.id,newMap);
                        return {modalOpen: false, simulations: prevState.simulations};
                    });
                })
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false});
        }
    }

    selectSimulation = (ev) => {
        const id = ev.currentTarget.dataset.simulationid;
        console.log(id)
    }

    startSimulation = (ev) =>{
        const id = ev.currentTarget.dataset.simulationid;
        console.log(id);
        axios.post(`http://localhost:8079/simulations/${id}/start`).then(res => {
            // debugger
        })

    }

    simulationList = () => {
        const list = [];
        for (const [i, simulation] of this.state.simulations) {
            console.log( simulation)
            list.push(
                <tr key={`${simulation}-${i}`} className={css.simulationItem} data-simulationid={i} onClick={this.selectSimulation}>
                    <td>{simulation.name}</td>
                    <td data-simulationid={simulation.id} onClick={this.startSimulation}><FaPlay /></td>
                    <td data-simulationid={simulation.id} onClick={this.openEdit}><FaRegEdit /></td>
                    <td data-simulationid={simulation.id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                </tr>
            )
        }
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, data, simulations, maps, clusters, vehicles, method, playing, warning} = this.state;

        return (
            <div className={css.simulationManager} {...rest}>
                <PageHeader title='Simulation Manager'>
                    <button onClick={this.openAddMewModal}>Add new</button>
                </PageHeader>
                <table>
                    <tbody>{simulations && this.simulationList()}</tbody>
                </table>
                { playing && <SimulationPlayer />}
                { modalOpen &&
                    <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Simulation'}>
                        <input
                            required
                            name="name"
                            type="text"
                            value={data.values.name}
                            placeholder="name"
                            onChange={this.handleInputChange} />
                        <Checkbox checked={data.values.apiOnly} label="API Only" name={'apiOnly'} onChange={this.handleInputChange} disabled={data.values.interactive}/>
                        <SingleSelect defaultValue={data.values.cluster} onChange={this.handleSelectInputChange} options={clusters} label="name" value="id" />
                        <SingleSelect defaultValue={data.map} onChange={this.handleSelectInputChange} placeholder={'Select a map'} options={maps} label="name" value="id" disabled={data.values.apiOnly} />
                        <SingleSelect defaultValue={data.vehicles} onChange={this.handleSelectInputChange} placeholder={'Select vehicles'} options={vehicles} label="name" value="id" disabled={data.values.apiOnly} />
                        <Checkbox checked={data.values.interactive} label="Interactive"  name={'interactive'} disabled={data.values.apiOnly || data.values.offScreen} />
                        <Checkbox checked={data.values.offScreen} label="Off-screen Rendering"  name={'offScreen'} disabled={data.values.interactive} />
                        <br />
                        <label className={appCss.inputLabel}>Time of day</label><br />
                        <input name="weather" type="text" value={data.values.timeOfDay || new Date()} onChange={this.handleInputChange} />
                        <br />
                        <label className={appCss.inputLabel}>Weather</label><br />
                        <label className={appCss.inputLabel}>min: 0, max: 1.0</label>
                        <input type="number" step="0.01" placeholder="cloudiness"/>
                        <input type="number" step="0.01" placeholder="rain"/>
                        <input type="number" step="0.01" placeholder="wetness"/>
                        <input type="number" step="0.01" placeholder="fog"/>
                        <span className={appCss.warning}>{warning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default SimulationManager;
