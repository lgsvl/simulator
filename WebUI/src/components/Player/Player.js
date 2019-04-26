import React from 'react'
import PropTypes from 'prop-types';
import {FaPlayCircle, FaRegPauseCircle} from 'react-icons/fa';
import css from './Player.module.less';
import classNames from 'classnames';
class SimulationPlayer extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string,
        open: PropTypes.bool,
        playing: PropTypes.bool,
        handlePlay: PropTypes.func,
        handlePause: PropTypes.func
    }

    handlePlay = () => {
        this.props.handlePlay();
    }

    handlePause = () => {
        this.props.handlePause();
    }

    render() {
        const {title, children, playing, ...rest} = this.props;
        delete rest.handlePlay;
        const classes = classNames(css.simulationPlayer, {[css.open]: true});
        return (
            <div className={classes} {...rest}>
                <span className={css.title}>{title}</span>
                {children}
                {playing ? <FaRegPauseCircle onClick={this.handlePause}/> : <FaPlayCircle onClick={this.handlePlay}/>}
            </div>
        )
    }
};

export default SimulationPlayer;