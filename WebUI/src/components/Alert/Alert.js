/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import PropTypes from 'prop-types';

import css from './Alert.module.less';
import classNames from 'classnames';

class Alert extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        type: PropTypes.string,
        msg: PropTypes.string
    }
    static defaultProps = {
        type: 'warning',
        msg: 'Something went wrong.'
    }

    render() {
        const {children, type, msg, ...rest} = this.props;
        const classes = classNames(css.alert, css[type.toLowerCase()]);

        return (
            <div className={classes} {...rest}>
                <span className={css.title}>{msg}</span>
                {children}
            </div>
        )
    }
};

export default Alert;