import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaRegWindowClose } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
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
        getList('vehicles').then(res => {
            if (res.data) {
                const vehicles = new Map(res.data.map(d => [d.id, d]));
                this.setState({vehicles});
            } else {
                this.setState({alert: true, alertType: res.name, alertMsg: res.message})
            }
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

    postVehicle = (data) => {
        postItem('vehicles', data.values).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newVehicle = res.data;
                if (newVehicle.responseStatus === 'error') {
                    this.setState({formWarning: newVehicle.error});
                } else if (newVehicle.status === 'success' || newVehicle.status === "1") {
                    this.setState(prevState => ({modalOpen: false, data: prevState.vehicles.set(newVehicle.id, newVehicle)}));
                }
            }
        });
    }

    editVehicle = (data) => {
        editItem('vehicles', data.id, data.values).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newVehicle = res.data;
                this.setState(prevState => {
                    prevState.vehicles.set(newVehicle.id,newVehicle);
                    return {modalOpen: false, vehicles: prevState.vehicles};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {data} = this.state;
        if (action === 'save') {
            if (this.state.method === 'POST') {
                this.postVehicle(data);
            } else if (this.state.method === 'PUT') {
                this.editVehicle(data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false});
        }
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, data: {values: {}}, method: 'POST'});
    }

    alertHide = () => {
        this.setState({alert: false});
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
        const {modalOpen, data, method, vehicles, formWarning, alert, alertType, alertMsg} = this.state;

        return (
            <div className={css.vehicleManager} {...rest}>
                {
                    alert &&
                    <Alert type={alertType} msg={alertMsg}>
                        <IoIosClose onClick={this.alertHide} />
                    </Alert>
                }
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
                        <span className={appCss.formWarning}>{formWarning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default VehicleManager;