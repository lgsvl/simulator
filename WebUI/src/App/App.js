import kind from '@enact/core/kind';
import React from 'react';
import {BrowserRouter as Router, Route} from 'react-router-dom';
import {FloatingLayerDecorator} from '@enact/ui/FloatingLayer';

import Home from '../views/Home';

import css from './App.module.less';

const App = kind({
	name: 'App',

	styles: {
		css,
		className: 'app'
	},

	render: (props) => (
		<Router>
			<div {...props}>
				<Route path="/" component={Home} />
			</div>
		</Router>
	)
});

// export default App;

export default FloatingLayerDecorator(App);
