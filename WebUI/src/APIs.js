import axios from 'axios';

const getList = (type) => {
    return axios.get(`http://localhost:8079/${type}`)
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
        .catch((err) => {
            console.log(err);
            return err;
        });
}

const getItem = (type, id) => {
    return axios.get(`http://localhost:8079/${type}/${id}`)
        .then(
            (response) => {
                if (response.status !== 200) {
                    console.log('Looks like there was a problem. Status Code: ' +
                    response.status);
                    return;
                }
                return response.data;
            }
        )
        .catch((err) => {
            console.log(err);
        });
}

const deleteItem = (type, id) => {
    return axios.delete(`http://localhost:8079/${type}/${id}`)
            .then(
                (response) => {
                    if (response.status !== 200) {
                        console.log('Looks like there was a problem. Status Code: ' +
                        response.status);
                        return;
                    }
                    return response.data;
                }
            )
            .catch((err) => {
                console.log(err);
            });
}

const postItem = (type, data) => {
    return axios.post(`http://localhost:8079/${type}`, data)
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
        .catch((err) => {
            console.log(err);
            return err;
        });
}

const editItem = (type, id, data) => {
    return axios.put(`http://localhost:8079/${type}/${id}`, data)
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
        .catch((err) => {
            console.log(err);
            return err;
        });
}
export {
    getList,
    getItem,
    deleteItem,
    postItem,
    editItem
}