#!/usr/bin/env node
// Minimal static file server for previewing docs/ locally: `npm run preview` or `node docs/serve.cjs [port]`.
// Dependency-free and written for old Node versions on purpose, because the desktop launcher may resolve an
// older `node` than the shell does. Not for production use.

const http = require("http");
const fs = require("fs");
const path = require("path");

const root = __dirname;
const port = Number(process.argv[2] || process.env.PORT || 8090);

const types = {
  ".html": "text/html; charset=utf-8",
  ".pdf": "application/pdf",
  ".md": "text/markdown; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".cjs": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
};

http
  .createServer(function (req, res) {
    var urlPath = decodeURIComponent((req.url || "/").split("?")[0]);
    if (urlPath === "/") urlPath = "/ARCHITECTURE.html";
    var file = path.normalize(path.join(root, urlPath));
    if (file.indexOf(root) !== 0 || file.indexOf("node_modules") !== -1) {
      res.writeHead(403);
      return res.end("Forbidden");
    }
    fs.stat(file, function (err, stat) {
      if (err || !stat.isFile()) {
        res.writeHead(404);
        return res.end("Not found: " + urlPath);
      }
      res.writeHead(200, {
        "Content-Type": types[path.extname(file).toLowerCase()] || "application/octet-stream",
        "Content-Length": stat.size,
        "Cache-Control": "no-store",
      });
      fs.createReadStream(file).pipe(res);
    });
  })
  .listen(port, function () {
    console.log("docs preview: http://localhost:" + port + "/ARCHITECTURE.html (and /ARCHITECTURE.pdf)");
  });
