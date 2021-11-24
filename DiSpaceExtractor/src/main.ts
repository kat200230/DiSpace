import * as Raw from './DiSpace_Raw';
import * as DiSpace from './DiSpace';
import * as Converter from './Converter';
import Writer from './DBWriter';
import * as SQLite3 from 'sqlite3';
import * as SQLite from 'better-sqlite3';
import * as fs from 'fs';


let input_json = fs.readFileSync("D:\\uni-repos\\DiSpace\\DiSpaceExtractor\\in\\970001_971000.json");
let input = JSON.parse(input_json as any) as Raw.GetAttemptAPIResult[];
// fs.writeFileSync("D:\\uni-repos\\DiSpace\\DiSpaceExtractor\\in\\970001_971000-compacted.json", JSON.stringify(input));

let converted = input.filter(i => !(i as any).error).map(a => {
  return Converter.convert_attempt(a as Raw.Attempt);
});

const options = {};
// options.verbose = console.log;
let db = new SQLite("D:\\uni-repos\\DiSpace\\dispace.sqlite", options);

let writer = new Writer(db);

db.transaction(() => {

  for (let [test, attempt] of converted) {

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




