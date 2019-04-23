import React from 'react'
import kind from '@enact/core/kind';
import Item from '@enact/ui/Item';
import PropTypes from 'prop-types';
import css from './Nav.module.less';

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
            console.log(event.target)
            const {
                disabled, onSelect
            } = props;

            if (disabled) {
                event.preventDefault();
                return;
            }

            if (onSelect) {
                onSelect(event.target.dataset.page);
            }
        }
    },

    computed: {
        navItems: ({items, onSelect}) => {
            return <ul>
                {items.map((item, i) => <li
                    key={`${item}-${i}`}
                    onClick={onSelect}
                    ><Item className={css.navItem} data-page={item.name}>{item.name}</Item>
                </li>)}
            </ul>
        }
    },

    render: ({position, navItems, ...rest}) => {
        return (
            <div className={css[`${position}-nav`]}>
                {navItems}
            </div>
        )
    }
});

export default Nav;