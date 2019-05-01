import React from 'react'
import PropTypes from 'prop-types';
import {FaPlayCircle, FaStopCircle} from 'react-icons/fa';
import css from './Player.module.less';
import classNames from 'classnames';
class SimulationPlayer extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string,
        open: PropTypes.bool,
        running: PropTypes.bool,
        handlePlay: PropTypes.func,
        handlePause: PropTypes.func,
        description: PropTypes.string
    }

    handlePlay = () => {
        // if (this.props.description !== 'Initializing') {
            this.props.handlePlay();
        // }
    }

    handlePause = () => {
        this.props.handlePause();
    }

    render() {
        const {title, children, description, ...rest} = this.props;
        delete rest.handlePlay;
        delete rest.handlePause;
        const running = description === 'Running';

        const playerClasses = classNames(css.simulationPlayer, {[css.open]: true});
        const playBtnClasses = classNames({[css.disabled]: description === 'Initializing'});
        return (
            <div className={playerClasses} {...rest}>
                <span className={css.title}>{title}</span>
                {children}
                <span className={css.description}>{description}</span>
                {running ? <FaStopCircle onClick={this.handlePause}/> : <FaPlayCircle className={playBtnClasses} onClick={this.handlePlay}/>}
            </div>
        )
    }
};

export default SimulationPlayer;