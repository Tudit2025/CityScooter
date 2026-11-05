#!/usr/bin/env node
// Build (optional) and deploy the Unity Web build to Vercel.
//
//   node tools/deploy-web.mjs --build          # Unity batchmode build, then deploy to production
//   node tools/deploy-web.mjs                  # deploy the existing Builds/Web to production
//   node tools/deploy-web.mjs --preview        # deploy as a preview URL instead
//   node tools/deploy-web.mjs --dry-run        # stage deploy/web and print vercel.json, no upload
//
// --build needs the Unity Editor CLOSED for this project (batchmode cannot open a locked project);
// otherwise use the menu CityScooter/Build Web first. Requires `npx vercel login` done once.
// Env: UNITY_EXE (default: Unity 6000.3.28f1 under Program Files), VERCEL_PROJECT (default: cityscooter).
import { execFileSync, spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const args = new Set(process.argv.slice(2));
const buildDir = path.join(repo, "Builds", "Web");
const stageDir = path.join(repo, "deploy", "web");
const project = process.env.VERCEL_PROJECT || "cityscooter";
const unityExe = process.env.UNITY_EXE || "C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe";

function fail(msg) {
  console.error("✗ " + msg);
  process.exit(1);
}

if (args.has("--build")) {
  console.log("• Unity batchmode Web build (takes a few minutes)…");
  const log = path.join(repo, "Logs", "web-build.log");
  fs.mkdirSync(path.dirname(log), { recursive: true });
  const r = spawnSync(unityExe, [
    "-batchmode", "-quit", "-projectPath", repo,
    "-executeMethod", "CityScooter.EditorTools.BuildScript.BuildWebBatch",
    ...(args.has("--dev") ? ["-cityscooterDev"] : []),
    "-logFile", log,
  ], { stdio: "inherit" });
  if (r.status !== 0) fail("Unity build failed (exit " + r.status + "), see " + log + ". Is the project open in the Editor?");
}

if (!fs.existsSync(path.join(buildDir, "index.html"))) fail("No build at " + buildDir + " — build first (--build or menu CityScooter/Build Web).");
const unityweb = fs.readdirSync(path.join(buildDir, "Build")).filter((f) => f.endsWith(".unityweb"));
if (!unityweb.length) fail("Build/ has no .unityweb files; expected Brotli + decompression fallback (see BuildScript.cs).");

// Stage: copy the build, keep the .vercel link folder between deploys.
fs.mkdirSync(stageDir, { recursive: true });
for (const entry of fs.readdirSync(stageDir)) {
  if (entry !== ".vercel") fs.rmSync(path.join(stageDir, entry), { recursive: true, force: true });
}
fs.cpSync(buildDir, stageDir, { recursive: true });

// Brotli files served with Content-Encoding so the browser decompresses natively (fast);
// the loader's decompression fallback still covers hosts without these headers (e.g. GitHub Pages).
const enc = (type) => [{ key: "Content-Encoding", value: "br" }, { key: "Content-Type", value: type }];
const vercelJson = {
  headers: [
    { source: "/Build/(.*).wasm.unityweb", headers: enc("application/wasm") },
    { source: "/Build/(.*).js.unityweb", headers: enc("application/javascript") },
    { source: "/Build/(.*).data.unityweb", headers: enc("application/octet-stream") },
  ],
};
fs.writeFileSync(path.join(stageDir, "vercel.json"), JSON.stringify(vercelJson, null, 2));
fs.writeFileSync(path.join(stageDir, ".vercelignore"), ".env*\n.vercel\n.vercelignore\n");
console.log("• Staged " + fs.readdirSync(stageDir).length + " entries in " + stageDir + " (" + unityweb.join(", ") + ")");

if (args.has("--dry-run")) {
  console.log(JSON.stringify(vercelJson, null, 2));
  process.exit(0);
}

const npx = process.platform === "win32" ? "npx.cmd" : "npx";
const vercel = (...a) => execFileSync(npx, ["--yes", "vercel@latest", ...a], { cwd: stageDir, encoding: "utf8", stdio: ["ignore", "pipe", "inherit"], shell: process.platform === "win32" });
if (!fs.existsSync(path.join(stageDir, ".vercel", "project.json"))) {
  console.log("• Linking " + stageDir + " to Vercel project '" + project + "'…");
  vercel("link", "--yes", "--project", project);
}
console.log("• Deploying (" + (args.has("--preview") ? "preview" : "production") + ")…");
const out = vercel("deploy", "--yes", ...(args.has("--preview") ? [] : ["--prod"]));
const urls = out.match(/https:\/\/[^\s"']+\.vercel\.app/g) || [];
console.log(out.trim().split("\n").slice(-3).join("\n"));
console.log("✓ Done. " + (args.has("--preview") ? "Preview" : "Production") + ": " + (args.has("--preview") ? urls[0] : "https://" + project + ".vercel.app") + (urls.length ? "" : "  (check the Vercel output above)"));
