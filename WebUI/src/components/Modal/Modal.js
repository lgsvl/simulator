/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import PropTypes from 'prop-types'
import FloatingLayer from '../FloatingLayer/FloatingLayer';

import css from './Modal.module.less';
class Modal extends React.Component {
    static propTypes = {
        open: PropTypes.bool,
        width: PropTypes.string,
        height: PropTypes.string
    }

    static defaultProps = {
        open: false,
        width: '450px',
        height: '500px'
    }

    onSave = () => {
        this.props.on();
        this.props.handleClose();
    }

    onCancel = () => {
        this.props.handleClose();
    }

    render() {
        const {open, onClick, children, ...rest} = this.props;
        delete rest.onModalClose;
        return (
            <div {...rest}>
                <FloatingLayer
                    open={open}
                    onClick={onClick}
                >
                <div className={css.modalContent}>
                    {children}
                </div>
            </FloatingLayer>
            </div>
        )
    }
};

export default Modal;