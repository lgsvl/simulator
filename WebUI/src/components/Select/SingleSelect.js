import React from 'react'
import PropTypes from 'prop-types';

import css from './Select.module.less';

class SingleSelect extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string
    }

    render() {
        const {options, label, value, placeholder, ...rest} = this.props;

        return (
            <select className={css.singleSelect} {...rest}>
                {placeholder && <option value="" disabled >{placeholder}</option>}
                {options && options.map(o => <option key={o[value]} value={o[value]}>{o[label]}</option>)}
            </select>
        )
    }
};

export default SingleSelect;