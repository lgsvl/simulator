This project was bootstrapped with [@enact/cli](https://github.com/enactjs/cli).

Below you will find some information on how to perform common tasks.
You can find the most recent version of this guide [here](https://github.com/enactjs/templates/blob/master/packages/moonstone/template/README.md).
Additional documentation on @enact/cli can be found [here](https://github.com/enactjs/cli/blob/master/docs/index.md).

## Folder Structure

After creation, your project should look like this:

```
my-app/
  README.md
  .gitignore
  node_modules/
  package.json
  src/
    App/
      App.js
      App.less
      package.json
    components/
    views/
      MainPanel.js
    index.js
  resources/
```

For the project to build, **these files must exist with exact filenames**:

* `package.json` is the core package manifest for the project
* `src/index.js` is the JavaScript entry point.

You can delete or rename the other files.

You can update the `license` entry in `package.json` to match the license of your choice. For more
information on licenses, please see the [npm documentation](https://docs.npmjs.com/files/package.json#license).

## Available Scripts

In the project directory, you can run:

### `npm run serve`

Builds and serves the app in the development mode.<br>
Open [http://localhost:8081](http://localhost:8081) to view it in the browser.

The page will reload if you make edits.<br>

### `npm run pack` and `npm run pack-p`

Builds the project in the working directory. Specifically, `pack` builds in development mode with code un-minified and with debug code included, whereas `pack-p` builds in production mode, with everything minified and optimized for performance. Be sure to avoid shipping or performance testing on development mode builds.

### `npm run watch`

Builds the project in development mode and keeps watch over the project directory. Whenever files are changed, added, or deleted, the project will automatically get rebuilt using an active shared cache to speed up the process. This is similar to the `serve` task, but without the http server.

### `npm run clean`

Deletes previous build fragments from ./dist.

### `npm run lint`

Runs the Enact configuration of Eslint on the project for syntax analysis.

### `npm run test` and `npm run test-watch`

These tasks will execute all valid tests (files that end in `-specs.js`) that are within the project directory. The `test` is a standard single execution pass, while `test-watch` will set up a watcher to re-execute tests when files change.

## Enact Build Options

The @enact/cli tool will check the project's `package.json` looking for an optional `enact` object for a few customization options:

* `template` _[string]_ - Filepath to an alternate HTML template to use with the [Webpack html-webpack-plugin](https://github.com/ampedandwired/html-webpack-plugin).
* `isomorphic` _[string]_ - Alternate filepath to a custom isomorphic-compatible entrypoint. Not needed if main entrypoint is already isomorphic-compatible.
* `title` _[string]_ - Title text that should be put within the HTML's `<title></title>` tags.
* `theme` _[object]_ - A simplified string name to extrapolate `fontGenerator`, `ri`, and `screenTypes` preset values from. For example, `"moonstone"`
* `fontGenerator` _[string]_ - Filepath to a commonjs fontGenerator module which will build locale-specific font CSS to inject into the HTML. By default will use any preset for a specified theme or fallback to moonstone.
* `ri` _[object]_ - Resolution independence options to be forwarded to the [LESS plugin](https://github.com/enyojs/less-plugin-resolution-independence). By default will use any preset for a specified theme or fallback to moonstone
* `screenTypes` _[array|string]_ - Array of 1 or more screentype definitions to be used with prerender HTML initialization. Can alternatively reference a json filepath to read for screentype definitons.  By default will use any preset for a specified theme or fallback to moonstone.
* `nodeBuiltins` _[object]_ - Configuration settings for polyfilling NodeJS built-ins. See `node` [webpack option](https://webpack.js.org/configuration/node/).
* `deep` _[string|array]_ - 1 or more javascript conditions that, when met, indicate deeplinking and any prerender should be discarded.
* `target` _[string|array]_ - A build-type generic preset string (see `target` [webpack option](https://webpack.js.org/configuration/target/)) or alternatively a specific [browserlist array](https://github.com/ai/browserslist) of desired targets.
* `proxy` _[string]_ - Proxy target during project `serve` to be used within the [http-proxy-middleware](https://github.com/chimurai/http-proxy-middleware).

For example:
```js
{
  ...
  "enact": {
    "theme": "moonstone",
    "nodeBuiltins": {
      fs: 'empty',
      net: 'empty',
      tls: 'empty'
    }
  }
  ...
}
```

## Displaying Lint Output in the Editor

Some editors, including Sublime Text, Atom, and Visual Studio Code, provide plugins for ESLint.

They are not required for linting. You should see the linter output right in your terminal as well as the browser console. However, if you prefer the lint results to appear right in your editor, there are some extra steps you can do.

You would need to install an ESLint plugin for your editor first.

>**A note for Atom `linter-eslint` users**

>If you are using the Atom `linter-eslint` plugin, make sure that **Use global ESLint installation** option is checked:

><img src="http://i.imgur.com/yVNNHJM.png" width="300">

Then, you will need to install some packages *globally*:

```sh
npm install -g eslint eslint-config-enact eslint-plugin-enact eslint-plugin-react eslint-plugin-babel babel-eslint eslint-plugin-jest

```

## Installing a Dependency

The generated project includes Enact (and all its libraries). It also includes React and ReactDOM.  For test writing, both Sinon and Enzyme are as development dependencies. You may install other dependencies with `npm`:

```
npm install --save <package-name>
```

## Importing a Component

This project setup supports ES6 modules thanks to Babel.
While you can still use `require()` and `module.exports`, we encourage you to use [`import` and `export`](http://exploringjs.com/es6/ch_modules.html) instead.

For example:

### `Button.js`

```js
import kind from '@enact/core/kind';

const Button = kind({
  render() {
    // ...
  }
});

export default Button; // Don’t forget to use export default!
```

### `DangerButton.js`


```js
import kind from '@enact/core/kind';
import Button from './Button'; // Import a component from another file

const DangerButton = kind({
  render(props) {
    return <Button {...props} color="red" />;
  }
});

export default DangerButton;
```

Be aware of the [difference between default and named exports](http://stackoverflow.com/questions/36795819/react-native-es-6-when-should-i-use-curly-braces-for-import/36796281#36796281). It is a common source of mistakes.

We suggest that you stick to using default imports and exports when a module only exports a single thing (for example, a component). That’s what you get when you use `export default Button` and `import Button from './Button'`.

Named exports are useful for utility modules that export several functions. A module may have at most one default export and as many named exports as you like.

Learn more about ES6 modules:

* [When to use the curly braces?](http://stackoverflow.com/questions/36795819/react-native-es-6-when-should-i-use-curly-braces-for-import/36796281#36796281)
* [Exploring ES6: Modules](http://exploringjs.com/es6/ch_modules.html)
* [Understanding ES6: Modules](https://leanpub.com/understandinges6/read#leanpub-auto-encapsulating-code-with-modules)

## Adding a LESS or CSS Stylesheet

This project setup uses [Webpack](https://webpack.github.io/) for handling all assets. Webpack offers a custom way of “extending” the concept of `import` beyond JavaScript. To express that a JavaScript file depends on a LESS/CSS file, you need to **import the CSS from the JavaScript file**:

### `Button.less`

```css
.button {
  padding: 20px;
}
```

### `Button.js`

```js
import kind from '@enact/core/kind';
import styles './Button.css'; // Tell Webpack that Button.js uses these styles

const Button = kind({
  render() {
    // You can use them as regular CSS styles
    return <div className={styles['button']} />;
  }
}
```

Upon importing a css/less files, the resulting object will be a mapping of class names from that document. This allows correct access to the class name string regardless how the build process mangles it up.

In development, expressing dependencies this way allows your styles to be reloaded on the fly as you edit them. In production, all CSS files will be concatenated into a single minified `.css` file in the build output.

Additionally, this system setup supports [CSS module spec](https://github.com/css-modules/css-modules) with allows for compositional CSS classes and inheritance of styles.

## Adding Images and Custom Fonts

With Webpack, using static assets like images and fonts works similarly to CSS.

You can **`import` an image right in a JavaScript module**. This tells Webpack to include that image in the bundle. Unlike CSS imports, importing an image or a font gives you a string value. This value is the final image path you can reference in your code.

Here is an example:

```js
import kind from '@enact/core/kind';
import logo from './logo.png'; // Tell Webpack this JS file uses this image

console.log(logo); // /logo.84287d09.png

const Header = kind({
  render: function() {
    // Import result is the URL of your image
    return <img src={logo} alt="Logo" />;
  }
});

export default Header;
```

This is currently required for local images. This ensures that when the project is built, webpack will correctly move the images into the build folder, and provide us with correct paths.

This works in LESS/CSS too:

```css
.logo {
  background-image: url(./logo.png);
}
```

Webpack finds all relative module references in CSS (they start with `./`) and replaces them with the final paths from the compiled bundle. If you make a typo or accidentally delete an important file, you will see a compilation error, just like when you import a non-existent JavaScript module. The final filenames in the compiled bundle are generated by Webpack from content hashes.
