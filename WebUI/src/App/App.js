import React from 'react';
import Favicon from 'react-favicon';
import {FloatingLayerDecorator} from '@enact/ui/FloatingLayer';
import css from './App.module.less';
import iconfile from '../../favicon.png'
import Home from '../views/Home';

class App extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
                <div {...this.props} className={css.app}>
                    <Favicon url={iconfile} />
                    <Home />
                </div>
        );
    }
}

export default FloatingLayerDecorator(App);
