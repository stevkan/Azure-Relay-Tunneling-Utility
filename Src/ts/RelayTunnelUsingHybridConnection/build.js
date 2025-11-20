const PELibrary = require("pe-library");
const ResEdit = require("resedit");
const fs = require("fs");
const path = require("path");

// Configuration
const exePath = "bin/win/RelayTunnelUsingHybridConnection.exe";
const outputPath = "bin/win/RelayTunnelUsingHybridConnection-win.exe";
const iconPath = "AzureRelayTunnelUtilityIcon.ico";

// Read version from package.json
const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const version = packageJson.version.replace(/-beta.*$/, ''); // Remove beta suffix for version numbers

const lang = 1033;       // en-US
const codepage = 1200;   // Unicode

// Check if executable exists
if (!fs.existsSync(exePath)) {
  console.error(`ERROR: Executable not found: ${exePath}`);
  console.error('Please build the executable first with: npm run build:release && pkg dist/index.js --targets node20-win-x64 --output bin/win/RelayTunnelUsingHybridConnection.exe');
  process.exit(1);
}

// Check if icon exists
if (!fs.existsSync(iconPath)) {
  console.error(`ERROR: Icon file not found: ${iconPath}`);
  process.exit(1);
}

console.log('======================================');
console.log('Post-processing Windows executable');
console.log('======================================');
console.log(`Reading executable: ${exePath}`);

let exeData, exe, res;
try {
  exeData = fs.readFileSync(exePath);
  exe = PELibrary.NtExecutable.from(exeData);
  res = PELibrary.NtExecutableResource.from(exe);
} catch (error) {
  console.error(`ERROR: Failed to read executable: ${error.message}`);
  process.exit(1);
}

console.log(`Loading icon: ${iconPath}`);
let iconFile, iconGroupIDs;
try {
  iconFile = ResEdit.Data.IconFile.from(fs.readFileSync(iconPath));
  iconGroupIDs = ResEdit.Resource.IconGroupEntry.fromEntries(res.entries).map((entry) => entry.id);
} catch (error) {
  console.error(`ERROR: Failed to load icon file: ${error.message}`);
  process.exit(1);
}

console.log('Setting icon...');
console.log(`  Found ${iconGroupIDs.length} existing icon group(s):`, iconGroupIDs);
try {
  if (iconGroupIDs.length > 0) {
    console.log(`  Replacing icon in group ID ${iconGroupIDs[0]}...`);
    ResEdit.Resource.IconGroupEntry.replaceIconsForResource(
      res.entries,
      iconGroupIDs[0],
      lang,
      iconFile.icons.map((item) => item.data)
    );
  } else {
    console.log('  No existing icon groups found, generating new icon group (ID 1)...');
    ResEdit.Resource.IconGroupEntry.generate(
      res.entries,
      1,  // Icon group ID (1 is standard for main application icon)
      lang,
      iconFile.icons.map((item) => item.data)
    );
  }
  console.log('  ✓ Icon set successfully');
} catch (error) {
  console.error(`ERROR: Failed to set icon: ${error.message}`);
  process.exit(1);
}

console.log('Updating version info...');
let viList, vi;
try {
  viList = ResEdit.Resource.VersionInfo.fromEntries(res.entries);
  if (!viList || viList.length === 0) {
    console.warn('  WARNING: No version info found, creating new...');
    vi = ResEdit.Resource.VersionInfo.createEmpty();
  } else {
    vi = viList[0];
  }
} catch (error) {
  console.error(`ERROR: Failed to get version info: ${error.message}`);
  process.exit(1);
}

const versionParts = version.split(".");
if (versionParts.length < 3) {
  console.error(`ERROR: Invalid version format: ${version}. Expected format: X.Y.Z`);
  process.exit(1);
}

const [major, minor, patch] = versionParts;
const majorNum = Number(major);
const minorNum = Number(minor);
const patchNum = Number(patch);

if (isNaN(majorNum) || isNaN(minorNum) || isNaN(patchNum)) {
  console.error(`ERROR: Version parts must be numbers: ${version}`);
  process.exit(1);
}

try {
  vi.setFileVersion(majorNum, minorNum, patchNum, 0, lang);
  vi.setProductVersion(majorNum, minorNum, patchNum, 0, lang);
} catch (error) {
  console.error(`ERROR: Failed to set version: ${error.message}`);
  process.exit(1);
}

try {
  vi.setStringValues({ lang, codepage }, {
    FileDescription: "Azure Relay Tunnel Utility - Hybrid Connection (Node.js)",
    ProductName: "Azure Relay Tunnel Utility (JS HC)",
    CompanyName: "",
    ProductVersion: packageJson.version,
    FileVersion: packageJson.version,
    OriginalFilename: path.basename(outputPath),
    LegalCopyright: `Steven Kanberg © ${new Date().getFullYear()}`
  });
} catch (error) {
  console.error(`ERROR: Failed to set string values: ${error.message}`);
  process.exit(1);
}

console.log('Generating updated executable...');
try {
  vi.outputToResourceEntries(res.entries);
  res.outputResource(exe);
  var newBinary = exe.generate();
} catch (error) {
  console.error(`ERROR: Failed to generate executable: ${error.message}`);
  process.exit(1);
}

console.log(`Writing to: ${outputPath}`);
try {
  fs.writeFileSync(outputPath, Buffer.from(newBinary));
  fs.rm(exePath, { force: true }, (err) => {
    if (err) {
      console.error(`ERROR: Failed to remove temporary executable: ${err.message}`);
      process.exit(1);
    }
  });
} catch (error) {
  console.error(`ERROR: Failed to write executable: ${error.message}`);
  process.exit(1);
}

console.log('');
console.log('✓ Package updated successfully!');
console.log(`  - Input: ${exePath}`);
console.log(`  - Output: ${outputPath}`);
console.log(`  - Icon: ${iconPath}`);
console.log(`  - Version: ${packageJson.version}`);
console.log('======================================');
