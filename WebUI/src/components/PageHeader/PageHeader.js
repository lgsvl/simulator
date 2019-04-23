import React from 'react'
import PropTypes from 'prop-types';

import css from './PageHeader.module.less';

class PageHeader extends React.Component {
    constructor(props) {
        super(props);
    }

    static propTypes = {
        title: PropTypes.string
    }

    render() {
        const {title, children, ...rest} = this.props;

        return (
            <div className={css.pageHeader} {...rest}>
                <span className={css.title}>{title}</span>
                {children}
            </div>
        )
    }
};

export default PageHeader;