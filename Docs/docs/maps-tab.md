# <a name="top"></a>Web UI Maps Tab Explanation

A `Map` can be in the following states. 
- A `Map` with a local URL or if it has already been downloaded will have a `Valid` status.
- If the URL to the `Map` assetbundle is not local and the assetbundle is not in the local database, then the vehicle needs to be downloaded.
Currently only 1 assetbundle is downloaded at a time. 
	- If an assetbundle is downloading, the `Map` will show a GREY dot and the status will be `Downloading` with the download percentage.
	- If another assetbundle is downloading, the icon will be ORANGE and the status will be `Downloading` without a percentage.
	- A downloading `Map` can be interrupted by pressing the stop button.
- If the `Map` is not usable in a Simulation it will have an `Invalid` status. This can be because the local assetbundle is not usable or the download was interrupted.

[![](images/web-map-states.png)](images/full_size_images/web-map-states.png)

### Where to find Maps <sub><sup>[top](#top)</sup></sub> {: #where-to-find-maps data-toc-label='Where to find Maps'}
Map AssetBundles and HD maps are available on our [content website](https://content.lgsvlsimulator.com/maps/). 
When adding a map, the link to the appropriate AssetBundle can be entered as the URL or the AssetBundle can be downloaded manually and the local path can be entered.

The HD maps for maps are available in the same page. Please see the relevant doc for instructions on how to add an HD map to an AD Stack:
- [Apollo 5.0](apollo5-0-instructions.md)
- [Apollo 3.0](apollo-instructions.md)
- [Autoware](autoware-instructions.md)

### How to Add a Map <sub><sup>[top](#top)</sup></sub> {: #how-to-add-a-map data-toc-label='How to Add a Map'}

1. Click the `Add new` button.
2. In the dialogue that opens, enter the name of the map and the URL to the AssetBundle. This can be a URL to a location in the cloud or to a location on a local drive.
3. If the URL is not local, the AssetBundle will be downloaded to the local database.

[![](images/web-add-map.png)](images/full_size_images/web-add-map.png)

### How to Edit a Map <sub><sup>[top](#top)</sup></sub> {: #how-to-edit-a-map data-toc-label='How to Edit a Map'}

1. Click the pencil icon
2. In the dialogue that opens, the name of the map can be changed and the URL to the AssetBundle.
3. If the URL is changed, the AssetBundle in the database will be updated (downloaded if necessary)

[![](images/web-edit-map.png)](images/full_size_images/web-edit-map.png)

### How to Annotate a Map <sub><sup>[top](#top)</sup></sub> {: #how-to-annotate-a-map data-toc-label='How to Annotate a Map'}
Please see [Map Annotation](map-annotation.md) for more information on how to annotate a map in Unity. This also details how to export or import and HD map.
