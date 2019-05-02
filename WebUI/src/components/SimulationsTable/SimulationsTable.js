import React from 'react'
import {FaRegEdit, FaRegWindowClose} from 'react-icons/fa';
import {Cell} from '@enact/ui/Layout';
import css from './SimulationsTable.module.less';
import classNames from 'classnames';
import PropTypes from 'prop-types';
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
            const classes = classNames(css.simulationItem, {[css.selected]: this.props.selected === i});
            const btnClassNames = classNames({[css.disabled]: blockingAction(simulation.status)})
            list.push(
                <tr key={`${simulation}-${i}`} className={classes} data-simulationid={i}>
                    <td data-simulationid={i} onClick={this.selectSimulation}>{simulation.name}</td>
                    <td>{simulation.status}</td>
                    <td data-simulationid={simulation.id} onClick={this.openEdit}><FaRegEdit className={btnClassNames} /></td>
                    <td data-simulationid={simulation.id} onClick={this.handleDelete}><FaRegWindowClose className={btnClassNames} /></td>
                </tr>
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
                    <table {...rest}>
                        <tbody>{this.simulationList()}</tbody>
                    </table>
                    :
                    <p>Please add a new Simulation.</p>
                }
            </Cell>
    }
};

export default SimulationsTable;
