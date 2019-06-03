import React from 'react';
import {FloatingLayerDecorator} from '@enact/ui/FloatingLayer';
import Home from '../views/Home';
import css from './App.module.less';

class App extends React.Component {
	constructor(props) {
		super(props);
	}

	render() {
		return (
				<div {...this.props} className={css.app}>
					<Home />
				</div>
		);
	}
}

export default FloatingLayerDecorator(App);
