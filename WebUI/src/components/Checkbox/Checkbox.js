/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import PropTypes from 'prop-types';

import css from './Checkbox.module.less';

class Checkbox extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        label: PropTypes.string,
        name: PropTypes.string,
        disabled: PropTypes.bool,
        checked: PropTypes.bool
    }

    render() {
        const {label, name, disabled, checked, ...rest} = this.props;

        return (
            <div className={css.checkbox} {...rest}>
                <input type="checkbox" id={label} name={name} disabled={disabled} defaultChecked={checked} />
                <label htmlFor={label}>{label}</label>
            </div>
        )
    }
};

export default Checkbox;