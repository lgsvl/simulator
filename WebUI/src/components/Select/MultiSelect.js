import React from 'react'
import PropTypes from 'prop-types';

import css from './Select.module.less';

class MultiSelect extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string,
        options: PropTypes.array
    }

    render() {
        const {options, label, value, defaultValue, placeholder, ...rest} = this.props;

        return (
            <select multiple className={css.multiSelect} {...rest}>
                {placeholder && <option value="" disabled defaultValue={defaultValue}>{placeholder}</option>}
                {options && options.map(o => <option key={o[value]} value={o[value]}>{o[label]}</option>)}
            </select>
        )
    }
};

export default MultiSelect;