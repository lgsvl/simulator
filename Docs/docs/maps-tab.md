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

### Where to find Maps [[top]] {: #where-to-find-maps data-toc-label='Where to find Maps'}
Map AssetBundles and HD maps are available on our [content website](https://content.lgsvlsimulator.com/maps/). 
When adding a map, the link to the appropriate AssetBundle can be entered as the URL or the AssetBundle can be downloaded manually and the local path can be entered.

The HD maps for maps are available in the same page. Please see the relevant doc for instructions on how to add an HD map to an AD Stack:

- [Apollo 5.0](apollo5-0-instructions.md)
- [Apollo 3.0](apollo-instructions.md)
- [Autoware](autoware-instructions.md)

### How to Add a Map [[top]] {: #how-to-add-a-map data-toc-label='How to Add a Map'}

1. Click the `Add new` button.
2. In the dialogue that opens, enter the name of the map and the URL to the AssetBundle. This can be a URL to a location in the cloud or to a location on a local drive.
3. If the URL is not local, the AssetBundle will be downloaded to the local database.

[![](images/web-add-map.png)](images/full_size_images/web-add-map.png)

### How to Edit a Map [[top]] {: #how-to-edit-a-map data-toc-label='How to Edit a Map'}

1. Click the pencil icon
2. In the dialogue that opens, the name of the map can be changed and the URL to the AssetBundle.
3. If the URL is changed, the AssetBundle in the database will be updated (downloaded if necessary)

[![](images/web-edit-map.png)](images/full_size_images/web-edit-map.png)

### How to Annotate a Map [[top]] {: #how-to-annotate-a-map data-toc-label='How to Annotate a Map'}
Please see [Map Annotation](map-annotation.md) for more information on how to annotate a map in Unity. This also details how to export or import and HD map.

### How to Migrate a Map [[top]] {: #how-to-annotate-a-map data-toc-label='How to Migrate Map'}
If you have created maps in our [old simulator](https://github.com/lgsvl/simulator-2019.05-obsolete) (before HDRP), you can reuse those maps in current HDRP simulator by following steps:

- In old simulator
	- You need to make some changes in header inside `HDMapTool.cs` (line 148-154):
		- Convert your MapOrigin Northing/Easting to Longitude/Latitude.
		- Replace values for `left` and `right` with Longitude.
		- Repalce values for `top` and `bottom` with Latitude.
		- You also need to change zone in header.
	![](images/migration-header.png)
	- If you have intersections annotated, you need to annotate junctions like [here](map-annotation.md#create-junction) for each intersection in the old simulator since we are importing intersections based on `junction` in HDRP simulator. Otherwise, signals/signs will not be grouped correctly, every signal/sign will be created as an intersection object.
	![](images/migration-junction.png)
	- Export map to Apollo Map file: `base_map.bin`.
	![](images/migration-export.png)
- In current HDRP simulator
	- Select `Simulator->Import HD Map`, and set `Import Format` as `Apollo 5 HD Map`.
	- Select the exported map and import.
	![](images/migration-imported.png)
- Note, you need to check intersections, some signals/signs may be grouped incorrectly.
