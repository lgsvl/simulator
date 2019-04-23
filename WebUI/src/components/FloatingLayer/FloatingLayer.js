import React from 'react'
import kind from '@enact/core/kind';
import UiFloatingLayer from '@enact/ui/FloatingLayer';
import css from './FloatingLayer.module.less';

const FloatingLayer = kind({
    name: 'AnimatedTooltip',
    styles: {
        css,
        className: 'floatingLayer'
    },
    render: (props) => {
        return <UiFloatingLayer {...props} />;
    }
});

export default FloatingLayer;