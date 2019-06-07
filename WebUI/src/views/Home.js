import React from 'react';
import {Row, Cell} from '@enact/ui/Layout';
import Nav from '../components/Nav/Nav.js';
import MapManager from '../components/MapManager/MapManager.js';
import VehicleManager from '../components/VehicleManager/VehicleManager.js';
import ClusterManager from '../components/ClusterManager/ClusterManager.js';
import SimulationManager from '../components/SimulationManager/SimulationManager.js';
import {HashRouter as Router, Route} from 'react-router-dom';
import {FaCar, FaMap, FaNetworkWired, FaRunning} from 'react-icons/fa';
import css from './Home.module.less';
import EventSource from 'eventsource';
import { SimulationProvider } from "../App/SimulationContext.js";

const items = [
	{name: 'Maps', icon: FaMap},
	{name: 'Vehicles', icon: FaCar},
	{name: 'Clusters', icon: FaNetworkWired},
	{name: 'Simulations', icon: FaRunning}
];

class Home extends React.Component {
	constructor(props) {
		super(props);
		this.state = {
			events: null,
			selected: 'maps'
		}
		this.eventSource = new EventSource('/events');
	}
	componentDidMount() {
		this.eventSource.addEventListener('simulation', (e) => this.handleSimEvents(e));
		this.eventSource.addEventListener('MapDownload', (e) => this.handleMapEvents(e));
		this.eventSource.addEventListener('VehicleDownload', (e) => this.handleVehEvents(e));
	}

	handleSimEvents = (e) => {
		this.setState({simulationEvents: e})
	};
	handleMapEvents = (e) => this.setState({mapDownloadEvents: e});
	handleVehEvents = (e) => this.setState({vehicleDownloadEvents: e});

	onSelect = (location, history) => (selected) => {
		const to = '/' + selected;
		if (location.pathname !== to) {
			history.push(to);
		}
		this.setState({selected});
	}

	MapManager= () => <MapManager />;
	VehicleManager= () => <VehicleManager />;
	ClusterManager= () => <ClusterManager />;
	SimulationManager= () => <SimulationManager />;

	routeRender = ({ location, history }) => {
		const {selected} = this.state;
		return <Row style={{height: '100%'}}>
			<Cell size={200}>
				<Nav
					position='side'
					items={items}
					onSelect={this.onSelect(location, history)}
					selected={selected}
				/>
			</Cell>
			<Cell>
				<main>
					<Route exact path='/' component={this.MapManager} />
					<Route path='/maps' component={this.MapManager} />
					<Route path='/vehicles' component={this.VehicleManager} />
					<Route path='/clusters' component={this.ClusterManager} />
					<Route path='/simulations' component={this.SimulationManager} />
				</main>
			</Cell>
		</Row>
	}

	render ({...rest}) {
		const {simulationEvents, mapDownloadEvents, vehicleDownloadEvents} = this.state;
		return <SimulationProvider value={{simulationEvents, mapDownloadEvents, vehicleDownloadEvents}}>
			<Router {...rest} className={css}>
				<Route render={this.routeRender} />
			</Router>
		</SimulationProvider>
	};
};

export default Home;
