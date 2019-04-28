import React from 'react';
import {Row, Cell} from '@enact/ui/Layout';
import Nav from '../components/Nav/Nav.js';
import MapManager from '../components/MapManager/MapManager.js';
import VehicleManager from '../components/VehicleManager/VehicleManager.js';
import ClusterManager from '../components/ClusterManager/ClusterManager.js';
import SimulationManager from '../components/SimulationManager/SimulationManager.js';
import {BrowserRouter as Router, Route} from 'react-router-dom';
import {FaCar, FaMap} from 'react-icons/fa';
import css from './Home.module.less';

const items = [
	{name: 'Map', icon: FaMap},
	{name: 'Vehicle', icon: FaCar},
	{name: 'Cluster'},
	{name: 'Simulation'}
];

const viewItems = (name) => {
	const result = [];
	for (let i = 0; i < 5; i++) {
		result.push({id: i, name: `${name} ${i}`});
	}
	return result;
}

class Home extends React.Component {
	constructor(props) {
		super(props);
	}

	onSelect = (location, history) => (selected) => {
		const to = '/' + selected;
		if (location.pathname !== to) {
			history.push(to);
		}
	}

	MapManager= () => <MapManager maps={viewItems('Map')}/>;
	VehicleManager= () => <VehicleManager cars={viewItems('Vehicle')}/>;
	ClusterManager= () => <ClusterManager clusters={viewItems('Cluster')}/>;
	SimulationManager= () => <SimulationManager simulations={viewItems('Simulation')}/>;

	routeRender = ({ location, history }) => {
		return <Row style={{height: '100%'}}>
			<Cell size={150}>
				<Nav
					position='side'
					items={items}
					onSelect={this.onSelect(location, history)}
				/>
			</Cell>
			<Cell>
				<main>
					<Route path='/map' component={this.MapManager} />
					<Route path='/vehicle' component={this.VehicleManager} />
					<Route path='/cluster' component={this.ClusterManager} />
					<Route path='/simulation' component={this.SimulationManager} />
				</main>
			</Cell>
		</Row>
	}

	render ({...rest}) {
		return <Router {...rest} className={css}>
			<Route render={this.routeRender} />
		</Router>
	};
};

export default Home;
