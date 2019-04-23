import React from 'react'
import PropTypes from 'prop-types'
import FloatingLayer from '../FloatingLayer/FloatingLayer';

import css from './Modal.module.less';
console.log(css.modalContent)
class Modal extends React.Component {
    static propTypes = {
        open: PropTypes.bool
    }

    onSave = () => {
        this.props.on();
        this.props.handleClose();
    }

    onCancel = () => {
        this.props.handleClose();
    }

    onClick = ({target}) => {
        console.log(target.className)
    }

    render() {
        const {open, onClick, children, ...rest} = this.props;
        return (
            <div>
                <FloatingLayer
                    // noAutoDismiss={noAutoDismiss}
                    open={open}
                    // onOpen={this.handleFloatingLayerOpen}
                    // onDismiss={onClose}
                    // scrimType='translucent'
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