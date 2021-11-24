import * as DiSpace from './DiSpace';
import * as SQLite from 'better-sqlite3';
import { Pars, Plac, Pack, Unpack, Diff } from './DBUtilities';


class DBWriter {
  db: SQLite.Database;

  _getUnitsByTestId?: SQLite.Statement;
  _getThemesByUnitId?: SQLite.Statement;
  _getQuestionsByThemeId?: SQLite.Statement;
  _getOptionsByQuestionId?: SQLite.Statement;

  _setTest?: SQLite.Statement;
  _setUnit?: SQLite.Statement;
  _setTheme?: SQLite.Statement;
  _setQuestion?: SQLite.Statement;
  _setOption?: SQLite.Statement;
  _setAttempt?: SQLite.Statement;
  _setUnitResult?: SQLite.Statement;
  _setThemeResult?: SQLite.Statement;
  _setAnswer?: SQLite.Statement;

  constructor(db: SQLite.Database) {
    this.db = db;

    this._getUnitsByTestId = db.prepare(`SELECT * FROM units WHERE test_id = ?;`).raw();
    this._getThemesByUnitId = db.prepare(`SELECT * FROM themes WHERE unit_id = ?;`).raw();
    this._getQuestionsByThemeId = db.prepare(`SELECT * FROM questions WHERE theme_id = ?;`).raw();
    this._getOptionsByQuestionId = db.prepare(`SELECT * FROM options WHERE question_id = ?;`).raw();

    this._setTest = db.prepare(`REPLACE INTO tests(${Pars.test}) VALUES(${Plac.test});`);
    this._setUnit = db.prepare(`REPLACE INTO units(${Pars.unit}) VALUES(${Plac.unit});`);
    this._setTheme = db.prepare(`REPLACE INTO themes(${Pars.theme}) VALUES(${Plac.theme});`);
    this._setQuestion = db.prepare(`REPLACE INTO questions(${Pars.question}) VALUES(${Plac.question});`);
    this._setOption = db.prepare(`REPLACE INTO options(${Pars.option}) VALUES(${Plac.option});`);
    
    this._setAttempt = db.prepare(`INSERT INTO attempts(${Pars.attempt}) VALUES(${Plac.attempt});`);
    this._setUnitResult = db.prepare(`INSERT INTO unit_results(${Pars.unitResult}) VALUES(${Plac.unitResult});`);
    this._setThemeResult = db.prepare(`INSERT INTO theme_results(${Pars.themeResult}) VALUES(${Plac.themeResult});`);
    this._setAnswer = db.prepare(`INSERT INTO answers(${Pars.answer}) VALUES(${Plac.answer});`);



  }
  private generateGet(table: string, id: string = "id") {
    return this.db.prepare(`SELECT * FROM ${table} WHERE ${id} = ? LIMIT 1;`).raw();
  }
  private generateGetMany(table: string, id: string = "id") {
    return this.db.prepare(`SELECT * FROM ${table} WHERE ${id} = ?;`).raw();
  }
  private generateSetLog(table: string, id: string = "id") {
    return this.db.prepare(`INSERT INTO ${table}(${id}, noticed_at, [index], field, old_value) VALUES($1, $2,
      (SELECT COALESCE( (SELECT [index] + 1 FROM ${table} WHERE ${id} = $1 AND noticed_at = $2 ORDER BY [index] DESC LIMIT 1), 0 )
      ), $3, $4);`);
  }

  _getTest?: SQLite.Statement;
  getTest(id: number, cascade: boolean = true): DiSpace.Test {
    this._getTest = this._getTest ?? this.generateGet("tests");
    let test = Unpack.test(this._getTest.get(id));
    if (test && cascade) test.units = this.getUnitsByTestId(id);
    return test;
  }
  _getUnit?: SQLite.Statement;
  getUnit(id: number, cascade: boolean = true): DiSpace.Unit {
    this._getUnit = this._getUnit ?? this.generateGet("units");
    let unit = Unpack.unit(this._getUnit.get(id));
    if (unit && cascade) unit.themes = this.getThemesByUnitId(id);
    return unit;
  }
  _getTheme?: SQLite.Statement;
  getTheme(id: number, cascade: boolean = true): DiSpace.Theme {
    this._getTheme = this._getTheme ?? this.generateGet("themes");
    let theme = Unpack.theme(this._getTheme.get(id));
    if (theme && cascade) theme.questions = this.getQuestionsByThemeId(id);
    return theme;
  }
  _getQuestion?: SQLite.Statement;
  getQuestion(id: number, cascade: boolean = true): DiSpace.Question {
    this._getQuestion = this._getQuestion ?? this.generateGet("questions");
    let question = Unpack.question(this._getQuestion.get(id));
    if (question && cascade) {
      const q = question as any;
      let options = this.getOptionsByQuestionId(id);
      if (question.type === 1 || question.type === 2 || question.type === 4)
        q.options = options;
      else if (question.type === 3) {
        q.rows = options.filter(o => (o as any).mapping);
        q.columns = options.filter(o => !(o as any).mapping);
      }
    }
    return question;
  }

  _getOption?: SQLite.Statement;
  getOption(question_id: number, hash: string): DiSpace.Option {
    this._getOption = this._getOption ?? this.db.prepare(`SELECT * FROM options WHERE question_id = ? AND hash = ? LIMIT 1;`).raw();
    return Unpack.option(this._getOption.get(question_id, hash));
  }
  _getAttempt?: SQLite.Statement;
  getAttempt(id: number): DiSpace.Attempt {
    this._getAttempt = this._getAttempt ?? this.generateGet("attempts");
    return Unpack.attempt(this._getAttempt.get(id));
  }
  
  getUnitsByTestId(test_id: number): DiSpace.Unit[] {
    return this._getUnitsByTestId.all(test_id).map(Unpack.unit).map(u => {
      u.themes = this.getThemesByUnitId(u.id);
      return u;
    });
  }
  getThemesByUnitId(unit_id: number): DiSpace.Theme[] {
    return this._getThemesByUnitId.all(unit_id).map(Unpack.theme).map(t => {
      t.questions = this.getQuestionsByThemeId(t.id);
      return t;
    });
  }
  getQuestionsByThemeId(theme_id: number): DiSpace.Question[] {
    return this._getQuestionsByThemeId.all(theme_id).map(Unpack.question).map((q: any) => {
      const options = this.getOptionsByQuestionId(q.id);
      if (q.type === 1 || q.type === 2 || q.type === 4)
        q.options = options;
      else if (q.type === 3) {
        q.rows = options.filter(o => (o as any).mapping);
        q.columns = options.filter(o => !(o as any).mapping);
      }
      return q;
    });
  }
  getOptionsByQuestionId(question_id: number): DiSpace.Option[] {
    return this._getOptionsByQuestionId.all(question_id).map(Unpack.option);
  }

  setTest(test: DiSpace.Test) { this._setTest.run(Pack.test(test)); }
  setUnit(unit: DiSpace.Unit) { this._setUnit.run(Pack.unit(unit)); }
  setTheme(theme: DiSpace.Theme) { this._setTheme.run(Pack.theme(theme)); }
  setQuestion(question: DiSpace.Question) { this._setQuestion.run(Pack.question(question)); }
  setOption(option: DiSpace.Option) { this._setOption.run(Pack.option(option)); }
  setAttempt(attempt: DiSpace.Attempt) { this._setAttempt.run(Pack.attempt(attempt)); }
  setUnitResult(unitResult: DiSpace.UnitResult) { this._setUnitResult.run(Pack.unitResult(unitResult)); }
  setThemeResult(themeResult: DiSpace.ThemeResult) { this._setThemeResult.run(Pack.themeResult(themeResult)); }
  setAnswer(answer: DiSpace.Answer) { this._setAnswer.run(Pack.answer(answer)); }

  _setTestLog?: SQLite.Statement;
  _setUnitLog?: SQLite.Statement;
  _setThemeLog?: SQLite.Statement;
  _setQuestionLog?: SQLite.Statement;
  _setAttemptLog?: SQLite.Statement;

  _setDiffs(statement: SQLite.Statement, diffs: Diff[], noticed_at: number) {
    this.db.transaction(() => {
      for (let diff of diffs) {
        statement.run({ 1: diff.id, 2: noticed_at, 3: diff.field, 4: diff.old_value });
      }
    })();
  }
  updateTest(test: DiSpace.Test, noticed_at: number) {
    let old = this.getTest(test.id, false);
    if (old) {
      let diffs = Diff.test(old, test);
      if (diffs.length == 0) return;
      this._setDiffs(this._setTestLog = this._setTestLog ?? this.generateSetLog("log_tests"), diffs, noticed_at);
    }
    this.setTest(test);
  }
  updateUnit(unit: DiSpace.Unit, noticed_at: number) {
    let old = this.getUnit(unit.id, false);
    if (old) {
      let diffs = Diff.unit(old, unit);
      if (diffs.length == 0) return;
      this._setDiffs(this._setUnitLog = this._setUnitLog ?? this.generateSetLog("log_units"), diffs, noticed_at);
    }
    this.setUnit(unit);
  }
  updateTheme(theme: DiSpace.Theme, noticed_at: number) {
    let old = this.getTheme(theme.id, false);
    if (old) {
      let diffs = Diff.theme(old, theme);
      if (diffs.length == 0) return;
      this._setDiffs(this._setThemeLog = this._setThemeLog ?? this.generateSetLog("log_themes"), diffs, noticed_at);
    }
    this.setTheme(theme);
  }
  updateQuestion(question: DiSpace.Question, noticed_at: number) {
    let old = this.getQuestion(question.id, false);
    if (old) {
      let diffs = Diff.question(old, question);
      if (diffs.length == 0) {
        if (question.type == 6 && old.max_score == null && question.max_score != null) {
          this.setQuestion(question);
          // set max_score now that it's known
        }
        return;
      }
      this._setDiffs(this._setQuestionLog = this._setQuestionLog ?? this.generateSetLog("log_questions"), diffs, noticed_at);
    }
    this.setQuestion(question);
  }

  _setOptionLog?: SQLite.Statement;
  updateOption(option: DiSpace.Option, noticed_at: number) {
    let old = this.getOption(option.question_id, option.hash);
    if (old) {
      let diffs = Diff.option(old, option);
      if (diffs.length == 0) return;

      this._setOptionLog = this._setOptionLog ?? this.db.prepare(`INSERT INTO log_options(question_id, hash, noticed_at, [index], field, old_value) VALUES($1, $2, $3,
        (SELECT COALESCE( (SELECT [index] + 1 FROM log_options WHERE question_id = $3 AND hash = $2 AND noticed_at = $3 ORDER BY [index] DESC LIMIT 1), 0 )
        ), $4, $5);`);

      this.db.transaction(() => {
        for (let diff of diffs) {
          this._setOptionLog.run({ 1: option.question_id, 2: option.hash, 3: noticed_at, 4: diff.field, 5: diff.old_value });
        }
      })();
    }
    this.setOption(option);
  }
  updateAttempt(attempt: DiSpace.Attempt, noticed_at: number) {
    let old = null;
    if (old) {
      let diffs = Diff.attempt(old, attempt);
      if (diffs.length == 0) return;
      this._setDiffs(this._setAttemptLog = this._setAttemptLog ?? this.generateSetLog("log_attempts"), diffs, noticed_at);
    }
    this.setAttempt(attempt);
  }
}

export default DBWriter;