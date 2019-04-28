import React from 'react'
import PropTypes from 'prop-types';

import css from './Alert.module.less';

class Alert extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        type: PropTypes.string,
        msg: PropTypes.string
    }

    render() {
        const {children, type, msg, ...rest} = this.props;

        return (
            <div className={css.alert} {...rest}>
                <span className={css.title}>{msg}</span>
                {children}
            </div>
        )
    }
};

export default Alert;