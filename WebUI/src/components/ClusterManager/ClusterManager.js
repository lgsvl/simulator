import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaRegWindowClose, FaRegPlusSquare } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import css from './ClusterManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'

const clusterData = {
    newName: null,
    newIps: ['']
}
class ClusterManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            clusters: [],
            ...Object.assign({}, clusterData)
        }
    }

    componentDidMount() {
        getList('clusters').then(res => {
            if (res.status === 200) {
                const clusters = new Map(res.data.map(d => [d.id, d]));
                this.setState({clusters});
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, method: 'POST', formWarning: '', ...Object.assign({}, clusterData)});
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.clusterid;
        getItem('clusters', id).then(res => {
            if (res.status === 200) {
                this.setState({modalOpen: true, ...res.data, method: 'PUT'})
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.clusterid;
        deleteItem('clusters', id).then(res => {
            if (res.status === 200) {
                this.setState(prevState => {
                    prevState.clusters.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.clusters}
                });
            } else {
                this.setState({alert: true, alertType: 'error', alertMsg: `${res.statusText}: ${res.data.error}`});
            }
        });
    }

    handleNameInputChange = (event) => {
        const target = event.currentTarget;
        this.setState({newName: target.value});
    }

    handleIPInputChange = (event) => {
        const target = event.currentTarget;

        this.setState(prevState => {
            prevState.newIps[target.dataset.ipid] = target.value;
            return {newIps: prevState.newIps};
        })
    }

    addIpField = () => {
        if (this.state.newIps.includes('')) {
            this.setState({formWarning: 'Use empty field first.'})
        } else {
            this.setState(prevState => {
                prevState.newIps.push('');
                return {
                    newIps: prevState.newIps,
                    formWarning: ''
                }
            })
        }
    }

    deleteIP = i => () => {
        this.setState(prevState => {
            prevState.newIps.splice(i, 1);
            return {newIps: prevState.newIps};
        })
    }

    postCluster = (data) => {
        postItem('clusters', data).then(res => {
            if (res.status !== 200) {
                this.setState({formWarning: res.data.error});
            } else {
                const newCluster = res.data;
                this.setState(prevState => ({modalOpen: false, data: prevState.clusters.set(newCluster.id, newCluster), formWarning: '', method: null}));
            }
        });
    }

    editCluster = (data) => {
        editItem('clusters', data.id, data).then(res => {
            if (res.status !== 200) {
                this.setState({formWarning: res.data.error});
            } else {
                const newCluster = res.data;
                this.setState(prevState => {
                    prevState.maps.set(newCluster.id, newCluster);
                    return {modalOpen: false, maps: prevState.maps, formWarning: '', method: null};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {newName, newIps, id} = this.state;
        const data = {
            id,
            name: newName,
            ips: newIps
        }
        if (action === 'save') {
            if (this.state.method === 'POST') {
                this.postCluster(data);
            } else if (this.state.method === 'PUT') {
                this.editCluster(data);
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false, formWarning: '', method: null});
        }
    }

    alertHide = () => {
        this.setState({alert: false});
    }

    clusterList = () => {
        const list = [];
        const {clusters} = this.state;
        clusters.forEach(({name, id, ips, status}, i) => {
            list.push(
                <tr key={`cluster-${id}`} className={css.clusterItem} data-clusterid={id}>
                    <td>{name}</td>
                    <td>{ips && <p>{ips.join(', ')}</p>}</td>
                    <td>{status}</td>
                    <td data-clusterid={id} onClick={this.openEdit}><FaRegEdit /></td>
                    {
                        i !== 0 &&
                        <td data-clusterid={id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                    }
                </tr>
            )
        })
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, clusters, method, formWarning, newName, newIps, alert, alertType, alertMsg} = this.state;

        return (
            <div className={css.ClusterManager} {...rest}>
                {
                    alert &&
                    <Alert type={alertType} msg={alertMsg}>
                        <IoIosClose onClick={this.alertHide} />
                    </Alert>
                }
                <PageHeader title='Cluster Manager'>
                    <button onClick={this.openAddMewModal}>Add new</button>
                </PageHeader>
                <table>
                    <thead><tr>
                        <th>Name</th>
                        <th>IP Addresses</th>
                        </tr></thead>
                    <tbody>{clusters && this.clusterList()}</tbody>
                </table>
                {   modalOpen &&
                    <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add new cluster'}>
                        <input
                            required
                            name="name"
                            type="text"
                            defaultValue={newName}
                            placeholder="name"
                            onChange={this.handleNameInputChange} />
                        {newIps.length > 0 &&
                            newIps.map((ip, i) => {
                                return <div
                                    className={css.inputWithButton}
                                    key={`ip-${i}`}
                                ><input
                                    name={`ip-${i}`}
                                    type="ips"
                                    defaultValue={ip}
                                    data-ipid={i}
                                    placeholder="ip"
                                    onChange={this.handleIPInputChange} /><FaRegWindowClose onClick={this.deleteIP(i)} />
                                </div>
                            })}
                        <FaRegPlusSquare onClick={this.addIpField}/>
                        <br />
                        <span className={appCss.formWarning}>{formWarning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default ClusterManager;