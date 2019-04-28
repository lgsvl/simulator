import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import { FaRegEdit, FaRegWindowClose, FaRegPlusSquare } from 'react-icons/fa';
import css from './ClusterManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'

class ClusterManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            clusters: [],
            data: {
                id: '',
                values: {
                    name: '',
                    ips: ''
                }
            },
            newName: null,
            newIps: ['']
        }
    }

    componentDidMount() {
        getList('clusters').then(data => {
            if (data) {
                const clusters = new Map(data.map(d => [d.id, d]));
                this.setState({clusters});
            }
        });
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, method: 'POST', warning: ''});
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.clusterid;
        getItem('clusters', id).then(data => {
            if(data) this.setState({modalOpen: true, newName: data.name, newIps: data.ips, id, method: 'PUT', warning: ''});
        });
    }

    handleDelete = (ev) => {
        const id = ev.currentTarget.dataset.clusterid;
        deleteItem('clusters', id).then(({responseStatus}) => {
            if (responseStatus === 'success') {
                this.setState(prevState => {
                    prevState.clusters.delete(parseInt(id));
                    return {modalOpen: false, data: prevState.clusters}
                });
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
            this.setState({warning: 'Use empty field first.'})
        } else {
            this.setState(prevState => {
                prevState.newIps.push('');
                return {
                    newIps: prevState.newIps,
                    warning: ''
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

    onModalClose = (action) => {
        const {newName, newIps, id} = this.state;
        const data = {
            id,
            name: newName,
            ips: newIps
        }
        if (action === 'save') {
            if (this.state.method === 'POST') {
                postItem('clusters', data).then(newCluster => {
                    if (newCluster.responseStatus === 'error') {
                        this.setState({warning: newCluster.error});
                    } else {
                        this.setState(prevState => ({modalOpen: false, data: prevState.clusters.set(newCluster.id, newCluster)}));
                    }
                })
            } else if (this.state.method === 'PUT') {
                editItem('clusters', data.id, data).then(newMap => {
                    this.setState(prevState => {
                        prevState.clusters.set(newMap.id,newMap);
                        return {modalOpen: false, clusters: prevState.clusters};
                    });
                })
            }
        } else if (action === 'cancel') {
            this.setState({modalOpen: false});
        }
    }

    clusterList = () => {
        const list = [];
        for (const [i, cluster] of this.state.clusters) {
            list.push(
                <tr key={`${cluster}-${i}`} className={css.clusterItem} data-clusterid={i}>
                    <td>{cluster.name}</td>
                    <td>{cluster.ips && <p>{cluster.ips.join(', ')}</p>}</td>
                    <td>{cluster.status}</td>
                    <td data-clusterid={cluster.id} onClick={this.openEdit}><FaRegEdit /></td>
                    <td data-clusterid={cluster.id} onClick={this.handleDelete}><FaRegWindowClose /></td>
                </tr>
            )
        }
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, clusters, method, warning, newName, newIps} = this.state;

        return (
            <div className={css.ClusterManager} {...rest}>
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
                            value={newName}
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
                                    value={ip}
                                    data-ipid={i}
                                    placeholder="ip"
                                    onChange={this.handleIPInputChange} /><FaRegWindowClose onClick={this.deleteIP(i)} />
                                </div>
                            })}
                        <FaRegPlusSquare onClick={this.addIpField}/>
                        <br />
                        <span className={appCss.warning}>{warning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default ClusterManager;