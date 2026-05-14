const webpackMerge = require("webpack-merge");

const baseWebpackConfig = require("@kentico/xperience-webpack-config");

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      orgName: "kcc",
      projectName: "resource-strings",
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
          test: /\.css$/,
          type: "asset/source",
        },
      ],
    },
    output: {
      clean: true,
    },
    devServer: {
      port: 3010,
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
};
