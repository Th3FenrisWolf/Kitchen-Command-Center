const webpackMerge = require("webpack-merge");

const baseWebpackConfig = require("@kentico/xperience-webpack-config");

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      // Sets the organizationName and projectName
      // The JS module is registered on the backend using these values
      orgName: "kcc",
      projectName: "admin",
      webpackConfigEnv: webpackConfigEnv,
      argv: argv,
    });
  };

  const projectConfig = {
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: "babel-loader",
        },
        {
          // Font Awesome (node_modules) CSS: inject into the DOM and resolve webfont url()s
          test: /\.css$/,
          include: /node_modules/,
          use: ["style-loader", "css-loader"],
        },
        {
          // Project CSS: imported as a string and injected via <style> (HomeTemplate pattern)
          test: /\.css$/,
          exclude: /node_modules/,
          type: "asset/source",
        },
      ],
    },
    output: {
      clean: true,
    },
    // Webpack server configuration. Required when running the boilerplate in 'Proxy' mode.
    devServer: {
      port: 3009,
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
};
