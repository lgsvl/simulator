/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import PropTypes from 'prop-types';
import {FaPlay, FaStop} from 'react-icons/fa';
import css from './Player.module.less';
import classNames from 'classnames';

const blockingAction = (status) => ['Starting', 'Stopping'].includes(status);
class SimulationPlayer extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            simulation: props.simulation,
            running: props.simulation.status === 'Running' || props.simulation.status === 'Stopping',
            blockAction: (props.simInProgress && props.simulation.id !== props.simInProgress)
            || (props.simulation.id === props.simInProgress && blockingAction(props.simulation.status))
        }
    }

    static propTypes = {
        simulation: PropTypes.object.isRequired,
        handlePlay: PropTypes.func,
        handlePause: PropTypes.func,
        simInProgress: PropTypes.number
    }

    static getDerivedStateFromProps(props) {
        return {
            running: props.simulation.status === 'Running' || props.simulation.status === 'Stopping',
            blockAction: (props.simInProgress && props.simulation.id !== props.simInProgress)
            || (props.simulation.id === props.simInProgress && blockingAction(props.simulation.status))
        }
    }

    handlePlay = () => {
        if (!this.state.blockAction) this.props.handlePlay();
    }

    handlePause = () => {
        if (!this.state.blockAction) this.props.handlePause();
    }

    render() {
        const {children, simulation, ...rest} = this.props;
        const {name, status} = simulation;
        const {blockAction, running} = this.state;
        delete rest.simInProgress;
        delete rest.handlePlay;
        delete rest.handlePause;

        const playerClasses = classNames(css.simulationPlayer, {[css.open]: true});
        const playBtnClasses = classNames({[css.disabled]: blockAction});
        const stopBtnClasses = classNames({[css.disabled]: blockAction});

        return <div className={playerClasses} {...rest}>
            <span className={css.title}>{name}</span>
            {children}
            <span className={css.status}>{status}</span>
            {running ?
                <FaStop className={stopBtnClasses} onClick={this.handlePause} />
                : <FaPlay className={playBtnClasses} onClick={this.handlePlay} />
            }
        </div>
    }
};

export default SimulationPlayer;