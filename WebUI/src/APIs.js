import axios from 'axios';

const getList = (type, cancelToken) => {
    return axios.get(`/${type}`, {cancelToken}, {cancelToken})
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return response;
            }
        )
        .catch(err => {
            if (axios.isCancel(err)) {
                console.log(`request cancelled: ${err.message}`);
            } else {
                console.log("another error happened:" + err.message);
            }
            return err.response || err;
        });
}

const getItem = (type, id, cancelToken) => {
    return axios.get(`/${type}/${id}`, {cancelToken})
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return response;
            }
        )
        .catch(err => {
            return err.response || err;
        });
}

const deleteItem = (type, id, cancelToken) => {
    return axios.delete(`/${type}/${id}`, {cancelToken})
            .then(
                (response) => {
                    if (response.status !== 200) {
                        console.log('Looks like there was a problem. Status Code: ' +
                        response.status);
                        return;
                    }
                    return response;
                }
            )
            .catch(err => {
                return err.response || err;
            });
}

const postItem = (type, data, cancelToken) => {
    return axios.post(`/${type}`, data, {cancelToken})
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return response;
            }
        )
        .catch(err => {
            return err.response || err;
        });
}

const editItem = (type, id, data, cancelToken) => {
    return axios.put(`/${type}/${id}`, data, {cancelToken})
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return  response;
            }
        )
        .catch(err => {
            return err.response || err;
        });
}

const patchItem = (type, id, data, cancelToken) => {
    return axios.patch(`/${type}/${id}`, data, {cancelToken})
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return  response;
            }
        )
        .catch(err => {
            return err.response || err;
        });
}

const stopDownloading = (type, id, cancelToken) => {
    return axios.put(`/${type}/${id}/cancel`, {cancelToken});
}

const restartDownloading = (type, id, cancelToken) => {
    return axios.put(`/${type}/${id}/download`, {cancelToken});
}

export {
    getList,
    getItem,
    deleteItem,
    postItem,
    editItem,
    patchItem,
    stopDownloading,
    restartDownloading
}