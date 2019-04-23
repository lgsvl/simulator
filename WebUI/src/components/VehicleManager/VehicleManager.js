import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import { FaRegEdit, FaRegWindowClose } from 'react-icons/fa';
import css from './VehicleManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'

class VehicleManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            vehicles: []
        }
    }

    componentDidMount() {
        getList('vehicles').then(data => {
            console.log(data)
            const vehicles = new Map(data.map(d => [d.id, d]));
            this.setState({vehicles});
        });
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.vehicleid;
        getItem('vehicles', id).then(data => {
            this.setState({modalOpen: true, data: {id, values: data}, method: 'PUT'})
        });
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.vehicleid;
        deleteItem('vehicles', id).then(({responseStatus}) => {
            if (responseStatus === 'success') {
                this.setState(prevState => {
                    prevState.vehicles.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.vehicles}
                });
            }
        });
    }

    handleInputChange = (event) => {
        const target = event.target;
        this.setState(prevState => ({
            data: {
                ...prevState.data,
                values: {
                    ...prevState.data.values,
                    [target.name]: target.value
                }
            }
        }));
    }

    onModalClose = (action) => {
        const {data} = this.state;
        if (action === 'save') {
            if (this.state.method === 'POST') {
                postItem('vehicles', data.values).then(newVehicle => {
                    if (newVehicle.responseStatus === 'error') {
                        this.setState({warning: newVehicle.error});
                    } else {
                        this.setState(prevState => ({modalOpen: false, data: prevState.vehicles.set(newVehicle.id, newVehicle)}));
                    }
                })
            } else if (this.state.method === 'PUT') {
                editItem('vehicles', data.id, data.values).then(newMap => {
                    this.setState(prevState => {
                        prevState.vehicles.set(newMap.id,newMap);
                        return {modalOpen: false, vehicles: prevState.vehicles};
                    });
                })
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false});
        }
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, data: {values: {}}, method: 'POST'});
    }

    vehicleList = () => {
        const list = [];
        for (const [i, vehicle] of this.state.vehicles) {
            list.push(
                <tr key={`${vehicle}-${i}`} className={css.vehicleItem} data-vehicleid={i}>
                    <td>{vehicle.name}</td>
                    <td>{vehicle.url}</td>
                    <td>{vehicle.status}</td>
                    <td data-vehicleid={vehicle.id} onClick={this.openEdit}><FaRegEdit /></td>
                    <td data-vehicleid={vehicle.id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                </tr>
            )
        }
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, data, method, vehicles, warning} = this.state;

        return (
            <div className={css.vehicleManager} {...rest}>
                <PageHeader title='Vehicle Manager'>
                    <button onClick={this.openAddMewModal}>Add new</button>
                </PageHeader>
                <table>
                <tbody>{vehicles && this.vehicleList()}</tbody>
                </table>
                {   modalOpen &&
                    <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Vehecle'}>
                        <input
                            required
                            name="name"
                            type="text"
                            value={data.values.name}
                            placeholder="name"
                            onChange={this.handleInputChange} />
                        <input
                            required
                            name="url"
                            type="url"
                            value={data.values.url}
                            placeholder="url"
                            onChange={this.handleInputChange} />
                        <span className={appCss.warning}>{warning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default VehicleManager;