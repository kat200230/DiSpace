import * as Raw from './DiSpace_Raw';
import * as DiSpace from './DiSpace';
import * as Converter from './Converter';
import Writer from './DBWriter';
import * as SQLite from 'better-sqlite3';
import * as fs from 'fs';

// let input_json = fs.readFileSync("D:\\uni-repos\\DiSpace\\DiSpaceExtractor\\in\\970001-971000.json");
// let input = JSON.parse(input_json as any) as Raw.GetAttemptAPIResult[];
// fs.writeFileSync("D:\\uni-repos\\DiSpace\\DiSpaceExtractor\\in\\970001_971000-compacted.json", JSON.stringify(input));

function convert(results: Raw.GetAttemptAPIResult[]) {
  let data: [DiSpace.Test, DiSpace.Attempt][] = [];
  for (let result of results) {
    if ((result as any).error) {
      // ignoring errors
      continue;
    }
    let pair = Converter.convert_attempt(result as Raw.Attempt);
    data.push(pair);
  }
  return data;
}
function convert_test_history(results: Raw.GetTestHistoryAPIResult[]) {
  let data: DiSpace.Test[] = [];
  for (let result of results) {
    if ((result as any).error) {
      // ignoring errors
      continue;
    }
    data.push(...Converter.extract_names(result as any));
  }
  return data;
}
function transfer(data: [DiSpace.Test, DiSpace.Attempt][]) {
  db.transaction(() => {
    
    for (let [test, attempt] of data) {
      const notice = attempt.started_at;
      
      try {
        writer.updateTest(test, notice);
        test.units.forEach(u => {
          writer.updateUnit(u, notice);
          u.themes.forEach(t => {
            writer.updateTheme(t, notice);
            t.questions.forEach(qu => {
              writer.updateQuestion(qu, notice);
              const q = qu as any;
              if (q.options) q.options.forEach(o => writer.updateOption(o, notice));
              if (q.rows) {
                q.rows.forEach(o => writer.updateOption(o, notice));
                q.columns.forEach(o => writer.updateOption(o, notice));
              }
            });
          });
        });
        writer.updateAttempt(attempt, notice);
        attempt.units.forEach(u => {
          writer.setUnitResult(u);
          u.themes.forEach(t => {
            writer.setThemeResult(t);
            t.answers.forEach(a => {
              writer.setAnswer(a);
            });
          });
        });
      } catch (ex) {
        console.log(ex);
        throw ex;
      }
      
    }

  })();
}
function transfer_test_history(data: DiSpace.Test[]) {
  db.transaction(() => {
    for (let test of data) {
      let old_test = writer.getTest(test.id, false);
      if (old_test == null || old_test.name != test.name) {
        writer.setTest(test);
      }
    }
  })();
}
function benchmark<T>(name: string, fn: () => T): T {
  let start = console.time(name);
  let res;
  try {
    res = fn();
  } catch (ex) {
    console.log(ex);
    throw ex;
  }
  console.timeEnd(name);
  return res;
}
const sleep = (ms: number) => new Promise(res => setTimeout(res, ms));

const options: SQLite.Options = {};
// options.verbose = console.log;
let db = new SQLite("D:\\uni-repos\\DiSpace\\dispace.sqlite", options);

let writer = new Writer(db);

let inDir = "D:\\uni-repos\\DiSpace\\DiSpaceExtractor\\in";
let inFiles = fs.readdirSync(inDir);

/*
let r = /th\.done\.(\d+)-(\d+)\.json/;
for (let f of inFiles) {
  let match = r.exec(f);
  if (match == null) continue;
  fs.renameSync(`${inDir}\\${f}`, `th.${inDir}\\${match[1]}-${match[2]}.json`);
}
throw new Error("Done!");
*/

async function doAttempts() {
  let nameRegex = /(\d+)-(\d+)\.json/;

  inFiles = inFiles.filter(f => nameRegex.test(f)).sort((a, b) => {
    let m1 = nameRegex.exec(a);
    let m2 = nameRegex.exec(b);
    return +m1[1] - +m2[1];
  });

  for (let inFile of inFiles) {
  
    let path = `${inDir}\\${inFile}`;
    let match = nameRegex.exec(inFile);
    if (match == null) continue;
    let start = +match[1];
    let end = +match[2];
    // if ((start % 50000) == 1) benchmark("Vacuuming", () => db.exec("VACUUM;"));
  
    console.log(`Working with ${inFile}`);
  
    let converted = benchmark("Convert", () => convert(JSON.parse(fs.readFileSync(path) as any) as Raw.GetAttemptAPIResult[]));
    benchmark("Transfer", () => transfer(converted));
  
    let renamedPath = `${inDir}\\done.${start}_${end}.json`;
    fs.renameSync(path, renamedPath);
  }
}

async function doTestHistories() {
  let nameRegex = /th\.(\d+)-(\d+)\.json/;

  inFiles = inFiles.filter(f => nameRegex.test(f)).sort((a, b) => {
    let m1 = nameRegex.exec(a);
    let m2 = nameRegex.exec(b);
    return +m1[1] - +m2[1];
  });

  for (let inFile of inFiles) {
  
    let path = `${inDir}\\${inFile}`;
    let match = nameRegex.exec(inFile);
    if (match == null) continue;
    let start = +match[1];
    let end = +match[2];
    // if ((start % 50000) == 1) benchmark("Vacuuming", () => db.exec("VACUUM;"));
  
    console.log(`Working with ${inFile}`);
  
    let converted = benchmark("Convert", () => convert_test_history(JSON.parse(fs.readFileSync(path) as any) as Raw.GetTestHistoryAPIResult[]));
    benchmark("Transfer", () => transfer_test_history(converted));
  
    let renamedPath = `${inDir}\\th.done.${start}_${end}.json`;
    fs.renameSync(path, renamedPath);
  }
}



(async () => {

  await doAttempts();
  // await doTestHistories();

})();



