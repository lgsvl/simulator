import React from 'react'
import Modal from '../Modal/Modal';
import PropTypes from 'prop-types';
import css from './Modal.module.less';

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

    render() {
        const {children, title, ...rest} = this.props;

        return (
                <Modal open {...rest}>
                    {title && <div className={css.modalTitle}>{title}</div>}
                    <div className={css.form}>
                        {children}
                        <div className={css.formFooter}>
                            <button className={css.submit} type="submit" onClick={this.onSave}>Submit</button>
                            <button onClick={this.onClose}>Cancel</button>
                        </div>
                    </div>
                </Modal>
        )
    }
};

export default FormModal;