import React from 'react'
import {FaRegEdit, FaRegWindowClose, FaRegCheckSquare} from 'react-icons/fa';
import {Cell} from '@enact/ui/Layout';
import css from './SimulationsTable.module.less';
import classNames from 'classnames';
import PropTypes from 'prop-types';
import appCss from '../../App/App.module.less';

const blockingAction = (status) => ['Running', 'Starting', 'Stopping'].includes(status);

class SimulationsTable extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        openEdit: PropTypes.func.isRequired,
        handleDelete: PropTypes.func.isRequired,
        selectSimulation: PropTypes.func.isRequired,
        selected: PropTypes.number
    }

    selectSimulation = (ev) => {
        const id = parseInt(ev.currentTarget.dataset.simulationid);
        this.props.selectSimulation(id);
    }

    openEdit = (ev) => {
        const id = parseInt(ev.currentTarget.dataset.simulationid);
        const status = this.props.simulations.get(id).status;
        if (!blockingAction(status)) this.props.openEdit(id);
    }

    handleDelete = (ev) => {
        const id = parseInt(ev.currentTarget.dataset.simulationid);
        const status = this.props.simulations.get(id).status;
        if (!blockingAction(status)) this.props.handleDelete(id);
    }

    simulationList() {
        const list = [];
        for (const [i, simulation] of this.props.simulations) {
            const simulationStatus = simulation.status || 'unknown';
            const classes = classNames(appCss.cardItem, css.simulationItem);
            const btnClassNames = classNames({[css.disabled]: blockingAction(simulationStatus)});
            const checkboxClassNames = classNames(appCss.cardSetting, {[css.selected]: this.props.selected === i});
            list.push(
                <div key={`${simulation}-${i}`} className={classes} data-simulationid={i} onClick={this.selectSimulation}>
                    <div className={appCss.cardName}>{simulation.name}</div>
                    <p className={appCss.cardBottom}>
                        <span className={classNames(appCss.statusDot, appCss[simulationStatus.toLowerCase()])} />
                        <span>{simulation.status}</span>
                    </p>
                    <div className={checkboxClassNames} data-simulationid={simulation.id}><FaRegCheckSquare className={btnClassNames} /></div>
                    <div className={appCss.cardEdit} data-simulationid={simulation.id} onClick={this.openEdit}><FaRegEdit className={btnClassNames} /></div>
                    <div className={appCss.cardDelete} data-simulationid={simulation.id} onClick={this.handleDelete}><FaRegWindowClose className={btnClassNames} /></div>
                </div>
            )
        }
        return list;
    }

    render() {
        const {simulations, ...rest} = this.props;
            delete rest.selectSimulation;
            delete rest.handleDelete;
            delete rest.openEdit;

            return <Cell>
                {simulations ?
                    <div className={appCss.cardItemContainer}>
                        {this.simulationList()}
                    </div>
                    :
                    <p>Please add a new Simulation.</p>
                }
            </Cell>
    }
};

export default SimulationsTable;
