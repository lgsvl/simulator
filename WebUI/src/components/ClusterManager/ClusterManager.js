/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import FormModal from '../Modal/FormModal';
import PageHeader from '../PageHeader/PageHeader';
import Alert from '../Alert/Alert';
import { FaRegEdit, FaRegWindowClose, FaRegPlusSquare } from 'react-icons/fa';
import {IoIosClose} from "react-icons/io";
import css from './ClusterManager.module.less';
import appCss from '../../App/App.module.less';
import {getList, getItem, deleteItem, postItem, editItem} from '../../APIs'
import classNames from 'classnames';
import axios from 'axios';

class ClusterManager extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            modalOpen: false,
            clusters: [],
            name: null,
            ips: ['']
        }
        this.source = axios.CancelToken.source();
        this.unmounted = false;
    }

    componentDidMount() {
        getList('clusters', this.source.token).then(res => {
            if (this.unmounted) return;
            if (res.status === 200) {
                const clusters = new Map(res.data.map(d => [d.id, d]));
                this.setState({clusters});
            } else {
                let alertMsg = 'Something went wrong.';
                if (res.name === "Error") {
                    alertMsg = res.message;
                } else if (res && res.data) {
                    alertMsg = `${res.statusText}: ${res.data.error}`;
                }
                this.setState({alert: true, alertType: 'error', alertMsg});
            }
        });
    }

    componentWillUnmount() {
        this.unmounted = true;
        this.source.cancel('Cancelling in cleanup.');
    }

    openAddMewModal = () => {
        this.setState({modalOpen: true, method: 'POST', formWarning: '', name: null, ips: ['']});
    }

    openEdit = (ev) => {
        const id = ev.currentTarget.dataset.clusterid;
        getItem('clusters', id, this.source.token).then(res => {
            if (this.unmounted) return;
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
            if (this.unmounted) return;
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
        this.setState({name: target.value});
    }

    handleIPInputChange = (event) => {
        const target = event.currentTarget;

        this.setState(prevState => {
            prevState.ips[target.dataset.ipid] = target.value;
            return {ips: prevState.ips};
        })
    }

    addIpField = () => {
        if (this.state.ips.includes('')) {
            this.setState({formWarning: 'Use empty field first.'})
        } else {
            this.setState(prevState => {
                prevState.ips.push('');
                return {
                    ips: prevState.ips,
                    formWarning: ''
                }
            })
        }
    }

    deleteIP = i => () => {
        this.setState(prevState => {
            prevState.ips.splice(i, 1);
            return {ips: prevState.ips};
        })
    }

    postCluster = (data) => {
        postItem('clusters', data).then(res => {
            if (this.unmounted) return;
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
            if (this.unmounted) return;
            if (res.status !== 200) {
                this.setState({formWarning: res.data.error});
            } else {
                const newCluster = res.data;
                this.setState(prevState => {
                    prevState.clusters.set(newCluster.id, newCluster);
                    return {modalOpen: false, clusters: prevState.clusters, formWarning: '', method: null};
                });
            }
        });
    }

    onModalClose = (action) => {
        const {name, ips, id} = this.state;
        const data = {
            id,
            name: name,
            ips: ips
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
        Array.from(clusters).forEach((cluster, i) => {
            const {name, id, ips, status} = cluster[1];
            list.push(
                <div key={`cluster-${id}`} className={appCss.cardItem} data-clusterid={id}>
                    <div className={appCss.cardName}>{name}</div>
                    {ips && <p>{ips.join(', ') || '127.0.0.1'}</p>}
                    <div>{status}</div>
                    <FaRegEdit className={classNames(appCss.cardEdit, {[css.hideBtn]: i === 0})} data-clusterid={id} onClick={this.openEdit} />
                    <FaRegWindowClose className={classNames(appCss.cardDelete, {[css.hideBtn]: i === 0})} data-clusterid={id} onClick={this.handleDelete} />
                </div>
            )
        })
        return list;
    }

    render() {
        const {...rest} = this.props;
        const {modalOpen, clusters, method, formWarning, name, ips, alert, alertType, alertMsg} = this.state;

        return (
            <div className={appCss.pageContainer} {...rest}>
                {
                    alert &&
                    <Alert type={alertType} msg={alertMsg}>
                        <IoIosClose onClick={this.alertHide} />
                    </Alert>
                }
                <PageHeader title='Clusters'>
                    <button className={appCss.primaryButton} onClick={this.openAddMewModal}>Add new</button>
                </PageHeader>
                <div className={appCss.cardItemContainer}>{clusters && this.clusterList()}</div>
                {   modalOpen &&
                    <FormModal onModalClose={this.onModalClose} title={method === 'PUT' ? 'Edit' : 'Add new cluster'}>
                        <h4 className={appCss.inputLabel}>
                            Cluster Name
                        </h4>
                        <input
                            required
                            name="name"
                            type="text"
                            defaultValue={name}
                            placeholder="name"
                            onChange={this.handleNameInputChange} />
                        <h4 className={appCss.inputLabel}>
                            Cluster Hosts
                        </h4>
                        <p className={appCss.inputDescription}>
                            Enter other host names or IP addresses to run distributed simulation.
                        </p>
                        {ips.length > 0 &&
                            ips.map((ip, i) => {
                                return <div
                                    className={css.inputWithButton}
                                    key={`ip-${i}`}
                                ><input
                                    name={`ip-${i}`}
                                    type="ips"
                                    defaultValue={ip}
                                    data-ipid={i}
                                    placeholder="ip address"
                                    onChange={this.handleIPInputChange} /><FaRegWindowClose onClick={this.deleteIP(i)} />
                                </div>
                            })}
                        <br />
                        <FaRegPlusSquare onClick={this.addIpField}/>
                        <p className={appCss.inputDescription}>Add more</p>
                        <br />
                        <span className={appCss.formWarning}>{formWarning}</span>
                    </FormModal>
                }
            </div>
        )
    }
};

export default ClusterManager;