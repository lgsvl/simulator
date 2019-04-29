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

    openAddMewModal = () => {
        this.setState({modalOpen: true, name: '', url: '', id: null, method: 'POST'});
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.mapid;
        getItem('maps', id).then(data => {
            this.setState({modalOpen: true, ...data, method: 'PUT'})
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
        this.setState({[target.name]: target.value});
    }

    postMap = (data) => {
        postItem('maps', data).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newMap = res.data;
                if (newMap.responseStatus === 'error') {
                    this.setState({formWarning: newMap.error});
                } else {
                    this.setState(prevState => ({modalOpen: false, data: prevState.maps.set(newMap.id, newMap), warning: '', method: null}));
                }
            }
        });
    }

    editMap = (id, data) => {
        editItem('maps', id, data).then(res => {
            if (!res.data) {
                this.setState({formWarning: res.message});
            } else {
                const newMap = res.data;
                if (newMap.responseStatus === 'error') {
                    this.setState({formWarning: newMap.error});
                } else {
                    this.setState(prevState => {
                        prevState.maps.set(newMap.id, newMap);
                        return {modalOpen: false, maps: prevState.maps, warning: '', method: null};
                    });
                }
            }
        });
    }

    onModalClose = (action) => {
        const {id, name, url} = this.state;
        const data = {name, url};
        if (action === 'save') {
            if (this.state.method === 'POST') {
                this.postMap(data);
            } else if (this.state.method === 'PUT') {
                this.editMap(id, data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false, warning: '', method: null});
        }
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
        const {modalOpen, name, url, maps, method, formWarning, alert, alertType, alertMsg} = this.state;

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
                            value={name}
                            placeholder="name"
                            onChange={this.handleInputChange} />
                        <input
                            name="url"
                            type="url"
                            value={url}
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