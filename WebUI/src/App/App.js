import React from 'react';
import {BrowserRouter as Router, Route} from 'react-router-dom';
import {FloatingLayerDecorator} from '@enact/ui/FloatingLayer';
import Home from '../views/Home';
import css from './App.module.less';

class App extends React.Component {
	constructor(props) {
		super(props);
	}

	render() {
		return (
			<Router>
				<div {...this.props} className={css.app}>
					<Route path="/" component={Home} />
				</div>
			</Router>
		);
	}
}

export default FloatingLayerDecorator(App);
