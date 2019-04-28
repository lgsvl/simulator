import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaRegWindowClose } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import css from './MapManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs.js'

class MapManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            maps: []
        }
    }

    componentDidMount() {
        getList('maps').then(res => {
            if (res.data) {
                const map = new Map(res.data.map(d => [d.id, d]));
                this.setState({maps: map});
            } else {
                this.setState({alert: true, alertType: res.name, alertMsg: res.message});
            }
        });
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.mapid;
        getItem('maps', id).then(data => {
            this.setState({modalOpen: true, data: {id, values: data}, method: 'PUT'})
        });
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.mapid;
        deleteItem('maps', id).then(({responseStatus}) => {
            if (responseStatus === 'success') {
                this.setState(prevState => {
                    prevState.maps.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.maps}
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

    postMap = (data) => {
        postItem('maps', data.values).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newMap = res.data;
                if (newMap.responseStatus === 'error') {
                    this.setState({formWarning: newMap.error});
                } else if (newMap.status === 'success' || newMap.status === "1") {
                    this.setState(prevState => ({modalOpen: false, data: prevState.maps.set(newMap.id, newMap)}));
                }
            }
        });
    }

    editMap = (data) => {
        editItem('maps', data.id, data.values).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newMap = res.data;
                this.setState(prevState => {
                    prevState.maps.set(newMap.id,newMap);
                    return {modalOpen: false, maps: prevState.maps};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {data} = this.state;
        if (action === 'save') {
            if (this.state.method === 'POST') {
                this.postMap(data);
            } else if (this.state.method === 'PUT') {
                this.editMap(data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false});
        }
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, data: {values: {}}, method: 'POST'});
    }

    mapList = () => {
        const list = [];
        for (const [i, map] of this.state.maps) {
            list.push(
                <tr key={`${map}-${i}`} className={css.mapItem} data-mapid={i}>
                    <td>{map.name}</td>
                    <td>{map.url}</td>
                    <td>{map.status}</td>
                    <td data-mapid={map.id} onClick={this.openEdit}><FaRegEdit /></td>
                    <td data-mapid={map.id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                </tr>
            )
        }
        return list;
    }

    alertHide = () => {
        this.setState({alert: false});
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, data, maps, method, formWarning, alert, alertType, alertMsg} = this.state;

        return (
            <div className={css.mapManager} {...rest}>
                {
                    alert &&
                    <Alert type={alertType} msg={alertMsg}>
                        <IoIosClose onClick={this.alertHide} />
                    </Alert>
                }
                <PageHeader title='Map Manager'>
                    <button onClick={this.openAddMewModal}>Add new</button>
                </PageHeader>
                <table>
                    <tbody>{maps && this.mapList()}</tbody>
                </table>
                {   modalOpen &&
                    <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add a new Map'}>
                        <input
                            name="name"
                            type="text"
                            value={data.values.name}
                            placeholder="name"
                            onChange={this.handleInputChange} />
                        <input
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

export default MapManager;