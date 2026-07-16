const { merge } = require("webpack-merge");
const baseWebpackConfig = require("@kentico/xperience-webpack-config");

// Shared webpack config for KCC Kentico admin client modules.
// The only per-module differences are the project name and the dev-server port.
function createAdminWebpackConfig({ projectName, port, orgName = "kcc" }) {
  return (opts, argv) =>
    merge(
      {
        module: {
          rules: [
            { test: /\.(js|ts)x?$/, exclude: [/node_modules/], loader: "babel-loader" },
            // Font Awesome (or any node_modules) CSS -> inject via <style>, resolve webfont url()s
            { test: /\.css$/, include: /node_modules/, use: ["style-loader", "css-loader"] },
            // Project CSS -> imported as a string and injected manually (HomeTemplate pattern)
            { test: /\.css$/, exclude: /node_modules/, type: "asset/source" },
          ],
        },
        output: { clean: true },
        devServer: { port },
      },
      baseWebpackConfig({ orgName, projectName, webpackConfigEnv: opts, argv })
    );
}

module.exports = { createAdminWebpackConfig };
