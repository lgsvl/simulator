/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

import React from 'react';
import Favicon from 'react-favicon';
import {FloatingLayerDecorator} from '@enact/ui/FloatingLayer';
import css from './App.module.less';
import iconfile from '../../favicon.png';
import Home from '../views/Home';
import Loading from '../views/Loading';
import {getList, editItem} from '../APIs';
import Url from 'url-parse';
import axios from 'axios';


class App extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            user: null,
        }
        this.token = null;
        this.source = axios.CancelToken.source();
        this.unmounted = false;

        const url = new Url(window.location, true);
        if ('token' in url.query) {
            this.token = url.query.token;
        }

        console.log("Token detected: ", this.token);
    }

    componentDidMount() {
        if (this.token) {
            console.log('Updating user with token...');
            editItem('users', encodeURIComponent(this.token)).then(() => {
                if (this.unmounted) return;
                console.log('Finished updating user with token: ', this.token);
                window.location.href = '/';
            });
        } else {
            console.log('Checkhing current user...');
            getList('users', this.source.token).then(response => {
                if (this.unmounted) return;
                console.log('Getting the user: ', response);
                if (response.status === 200) {
                    console.log('Successufully got user: ', response.data);
                    this.setState({ user: response.data });
                } else {
                    console.log('Require authentication in the cloud', response);
                    if (response.status === 401) {
                        // User is not authenticated, let's run through cloud authentication
                        console.log('Redirecting to: ',  response.data.cloudUrl)
                        window.location.href = `${response.data.cloudUrl}?returnUrl=${encodeURIComponent(window.location.href)}`;
                    } else {
                        // Something went horribly wrong
                        console.log('Failed to get user information');
                    }
                }
            });
        }
    }

    componentWillUnmount() {
        this.unmounted = true;
        this.source.cancel('Cancelling in cleanup.');
    }

    render() {
        const home = ( this.state.user ? <Home user={this.state.user}/> : <Loading /> );
        return (
            <div {...this.props} className={css.app}>
                <Favicon url={iconfile} />
                {home}
            </div>
        );
    }
}

export default FloatingLayerDecorator(App);
