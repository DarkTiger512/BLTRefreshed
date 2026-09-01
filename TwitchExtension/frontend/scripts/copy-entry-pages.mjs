import { copyFile } from "node:fs/promises";
for (const file of ["viewer.html", "config.html", "live-config.html"]) await copyFile("dist/index.html", `dist/${file}`);
