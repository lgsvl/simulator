/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import kind from '@enact/core/kind';
import Item from '@enact/ui/Item';
import PropTypes from 'prop-types';
import css from './Nav.module.less';
import classNames from 'classnames';

const Nav = kind({
    name: 'Nav',

    propTypes: {
        items: PropTypes.array.isRequired,
        position: PropTypes.string
    },

    styles: {
        css,
        className: 'nav'
    },

    handlers: {
        onSelect: (event, props) => {
            const {
                disabled, onSelect
            } = props;

            if (disabled) {
                event.preventDefault();
                return;
            }

            if (onSelect) {
                onSelect(event.currentTarget.dataset.page);
            }
        }
    },

    computed: {
        navItems: ({items, onSelect, selected}) => {
            return <ul>
                {items.map((item, i) => {
                    const classes = classNames(css.navItem, {[css.selectedItem]: selected.toLowerCase() === item.name.toLowerCase()});
                    return <li
                        key={`${item}-${i}`}
                        onClick={onSelect}
                        className={classes}
                        data-page={item.name}
                        >
                            {item.icon()}
                            <Item>{item.name}</Item>
                    </li>
                })}
            </ul>
        }
    },

    render: ({position, navItems, ...rest}) => {
        return (
            <div {...rest} className={css[`${position}-nav`]}>
                {navItems}
            </div>
        )
    }
});

export default Nav;