import React from 'react';
import {Row, Cell} from '@enact/ui/Layout';
import Nav from '../components/Nav/Nav.js';
import MapManager from '../components/MapManager/MapManager.js';
import VehicleManager from '../components/VehicleManager/VehicleManager.js';
import ClusterManager from '../components/ClusterManager/ClusterManager.js';
import SimulationManager from '../components/SimulationManager/SimulationManager.js';
import {HashRouter as Router, Route} from 'react-router-dom';
import {FaCar, FaMap} from 'react-icons/fa';
import css from './Home.module.less';
import EventSource from 'eventsource';
import { SimulationProvider } from "../App/SimulationContext";

const items = [
	{name: 'Map', icon: FaMap},
	{name: 'Vehicle', icon: FaCar},
	{name: 'Cluster'},
	{name: 'Simulation'}
];

class Home extends React.Component {
	constructor(props) {
		super(props);
		this.state = {
			events: null
		}
		this.eventSource = new EventSource("http://localhost:8079/events");
	}
	componentDidMount() {
        this.eventSource.addEventListener("SimulationUpdate", (e) => this.handleEvents(e));
	}

	handleEvents = (e) => this.setState({events: e});

	onSelect = (location, history) => (selected) => {
		const to = '/' + selected;
		if (location.pathname !== to) {
			history.push(to);
		}
	}

	MapManager= () => <MapManager />;
	VehicleManager= () => <VehicleManager />;
	ClusterManager= () => <ClusterManager />;
	SimulationManager= () => <SimulationManager />;

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
		const {events} = this.state;
		return <SimulationProvider value={{events}}><Router {...rest} className={css}>
			<Route render={this.routeRender} />
		</Router></SimulationProvider>
	};
};

export default Home;
