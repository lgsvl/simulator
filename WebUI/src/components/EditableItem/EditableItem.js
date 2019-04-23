import React from 'react'
import kind from '@enact/core/kind';
import IconButton from '@enact/ui/IconButton';
import Button from '@enact/moonstone/Button';
import Item from '@enact/ui/Item';
import PropTypes from 'prop-types';
import css from './EditableItem.module.less';

const EditableItem = kind({
    name: 'EditableItem',

    propTypes: {
        items: PropTypes.array.isRequired,
        position: PropTypes.string
    },

    styles: {
        css,
        className: 'EditableItem'
    },

    render: ({children, ...rest}) => {
        return (
            <Item>
                {children}

            </Item>
        )
    }
});

export default EditableItem;