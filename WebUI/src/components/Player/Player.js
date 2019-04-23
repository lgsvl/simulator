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
        handlePlay: PropTypes.func
    }

    handlePlay = () => {
        this.props.handlePlay();
    }

    render() {
        const {title, children, open, playing, ...rest} = this.props;
        const classes = classNames(css.simulationPlayer, {[css.open]: open});
        return (
            <div className={classes} {...rest}>
                <span className={css.title}>{title}</span>
                {children}
                {playing ? <FaRegPauseCircle onClick={this.handlePlay}/> : <FaPlayCircle onClick={this.handlePlay}/>}
            </div>
        )
    }
};

export default SimulationPlayer;