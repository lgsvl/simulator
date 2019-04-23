import React from 'react'
import PropTypes from 'prop-types';

import css from './Select.module.less';

class PageHeader extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string
    }

    render() {
        const {options, label, value, defaultValue, placeholder, ...rest} = this.props;
console.log(defaultValue)
        return (
            <select className={css.singleSelect} {...rest}>
                {placeholder && <option value="" disabled defaultValue={defaultValue}>{placeholder}</option>}
                {options && options.map(o => <option key={o[value]} value={o[value]}>{o[label]}</option>)}
            </select>
        )
    }
};

export default PageHeader;