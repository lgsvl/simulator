/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react'
import Modal from '../Modal/Modal';
import PropTypes from 'prop-types';
import css from './Modal.module.less';
import classnames from 'classnames';

class FormModal extends React.Component {
    constructor(props) {
        super(props);
    }
    static propTypes = {
        open: PropTypes.bool,
        title: PropTypes.string
    }

    onClose = () => {
        this.props.onModalClose('cancel');
    }

    onSave = () => {
        this.props.onModalClose('save');
    }

    onKeyDown = (event) => {
        //Checking for the "Esc" key.
        if (event.keyCode === 27) {
            this.onClose();
        }
    }

    componentDidMount() {
        window.addEventListener('keydown', this.onKeyDown, false);
    }

    componentWillUnmount() {
        window.removeEventListener('keydown', this.onKeyDown);
    }

    render() {
        const {children, title, ...rest} = this.props;

        return (
                <Modal open {...rest}>
                    {title && <div className={css.modalTitle}>{title}</div>}
                    <div className={css.form}>
                        {children}
                        <div className={css.formFooter}>
                            <button className={classnames(css.actionButton, css.submit)} type="submit" onClick={this.onSave}>Submit</button>
                            <button className={classnames(css.actionButton)} onClick={this.onClose}>Cancel</button>
                        </div>
                    </div>
                </Modal>
        )
    }
};

export default FormModal;