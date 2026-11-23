const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const skip = new Set(['__pycache__', 'node_modules', '.git', '.tools', 'CharactersPSX', 'CharactersPSX.meta', 'AsylumKit', 'AsylumKit.meta']);
const extensions = new Set(['.cs','.meta','.png','.fbx','.blend','.svg','.py','.md','.json','.asset','.asmdef','.txt','.cmd','.ps1','.cjs','.unity','.prefab','.mat','.obj','.target']);
const files = [];
function walk(folder) {
  for (const item of fs.readdirSync(folder, {withFileTypes:true})) {
    if (skip.has(item.name)) continue;
    const file = path.join(folder,item.name);
    const actual = fs.realpathSync(file);
    if (!actual.toLowerCase().startsWith(root.toLowerCase()+path.sep)) throw new Error('Export path escaped project');
    if (fs.statSync(file).isDirectory()) walk(file);
    else if (extensions.has(path.extname(file).toLowerCase())) files.push(path.relative(root,file));
  }
}
for (const name of ['Assets','Packages','ProjectSettings','scripts','docs','art']) walk(path.join(root,name));
for (const name of ['README.md','EXPORT_STATUS.md','Play Hide iT.cmd','Open HIde IT.cmd','.gitignore'])
  if (fs.existsSync(path.join(root,name))) files.push(name);
const target = path.join(root,'Exports','source-files.json');
fs.mkdirSync(path.dirname(target), {recursive:true});
fs.writeFileSync(target,JSON.stringify(files,null,2));
console.log(`Prepared ${files.length} source/art entries.`);
