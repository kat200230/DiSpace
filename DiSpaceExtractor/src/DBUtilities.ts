import * as DiSpace from './DiSpace';

export type Diff = {
  id: any,
  field: number,
  old_value: any,
}
export type DataRow = (string | number | null)[];

function makeParameters(properties: string[]): [string, string] {
  return [
    properties.join(","),
    properties.map(p => "?").join(",")
  ];
}
function packString(str: string): string {
  return str.replace(/\//g, "&s;").replace(/\|/g, "&p;");
}
function unpackString(str: string): string {
  return str.replace(/&s;/g, "/").replace(/&p;/g, "|");
}

const [testParameters, testPlaceholders] = makeParameters([
  "id",
  "name",
]);
function packTest(test: DiSpace.Test): DataRow {
  return [
    test.id,
    test.name,
  ];
}
function unpackTest(data: any[]): DiSpace.Test {
  if (data == null) return null;
  return {
    id: data[0],
    name: data[1],
    units: undefined,
  };
}
function diffTest(a: DiSpace.Test, b: DiSpace.Test): Diff[] {
  let diffs: Diff[] = [];
  if (a.name !== b.name) diffs.push({ id: b.id, field: 2, old_value: a.name });
  return diffs;
}

const [unitParameters, unitPlaceholders] = makeParameters([
  "id",
  "test_id",
  "hash",
  "name",
  "description",
  "selection",
  "is_visible",
  "is_shuffled",
]);
function packUnit(unit: DiSpace.Unit): DataRow {
  return [
    unit.id,
    unit.test_id,
    unit.hash,
    unit.name,
    unit.description,
    unit.selection,
    unit.is_visible ? 1 : 0,
    unit.is_shuffled ? 1 : 0,
  ];
}
function unpackUnit(data: any[]): DiSpace.Unit {
  if (data == null) return null;
  return {
    id: data[0],
    test_id: data[1],
    hash: data[2],
    name: data[3],
    description: data[4],
    selection: data[5],
    is_visible: !!data[6],
    is_shuffled: !!data[7],
    themes: undefined,
  };
}
function diffUnit(a: DiSpace.Unit, b: DiSpace.Unit): Diff[] {
  let diffs: Diff[] = [];
  if (a.hash !== b.hash) diffs.push({ id: b.id, field: 2, old_value: a.hash });
  if (a.name !== b.name) diffs.push({ id: b.id, field: 3, old_value: a.name });
  if (a.description !== b.description) diffs.push({ id: b.id, field: 4, old_value: a.description });
  if (a.selection !== b.selection) diffs.push({ id: b.id, field: 5, old_value: a.selection });
  if (a.is_visible !== b.is_visible) diffs.push({ id: b.id, field: 6, old_value: a.is_visible ? 1 : 0 });
  if (a.is_shuffled !== b.is_shuffled) diffs.push({ id: b.id, field: 7, old_value: a.is_shuffled ? 1 : 0 });
  return diffs;
}

const [themeParameters, themePlaceholders] = makeParameters([
  "id",
  "unit_id",
  "hash",
  "name",
  "description",
  "selection",
  "is_visible",
  "is_shuffled",
]);
function packTheme(theme: DiSpace.Theme): DataRow {
  return [
    theme.id,
    theme.unit_id,
    theme.hash,
    theme.name,
    theme.description,
    theme.selection,
    theme.is_visible ? 1 : 0,
    theme.is_shuffled ? 1 : 0,
  ];
}
function unpackTheme(data: any[]): DiSpace.Theme {
  if (data == null) return null;
  return {
    id: data[0],
    unit_id: data[1],
    hash: data[2],
    name: data[3],
    description: data[4],
    selection: data[5],
    is_visible: !!data[6],
    is_shuffled: !!data[7],
    questions: undefined,
  };
}
function diffTheme(a: DiSpace.Theme, b: DiSpace.Theme): Diff[] {
  let diffs: Diff[] = [];
  if (a.hash !== b.hash) diffs.push({ id: b.id, field: 2, old_value: a.hash });
  if (a.name !== b.name) diffs.push({ id: b.id, field: 3, old_value: a.name });
  if (a.description !== b.description) diffs.push({ id: b.id, field: 4, old_value: a.description });
  if (a.selection !== b.selection) diffs.push({ id: b.id, field: 5, old_value: a.selection });
  if (a.is_visible !== b.is_visible) diffs.push({ id: b.id, field: 6, old_value: a.is_visible ? 1 : 0 });
  if (a.is_shuffled !== b.is_shuffled) diffs.push({ id: b.id, field: 7, old_value: a.is_shuffled ? 1 : 0 });
  return diffs;
}

const [questionParameters, questionPlaceholders] = makeParameters([
  "id",
  "theme_id",
  "title",
  "prompt",
  "max_score",
  "show_solution",
  "type_original",
  "type",
  "is_shuffled",
  "modal_feedback",
  "max_choices",
  "correct",
]);
function packCorrect(q: any): string {
  return q.type === 2 ? (q.correct as [string, string][]).map(p => p.join(";")).join("|")
    : q.type === 4 ? (q.correct as string[]).join("|")
    : q.type === 5 ? q.correct.map(o => `${packString(o.pattern)}/${o.score}`).join("|")
    : null;
}
function packQuestion(question: DiSpace.Question): DataRow {
  const q = question as any;

  return [
    question.id,
    question.theme_id,
    question.title,
    question.prompt,
    question.max_score,
    question.show_solution ? 1 : 0,
    question.type_original,
    question.type,
    q.is_shuffled !== undefined ? q.is_shuffled ? 1 : 0 : null,
    q.modal_feedback,
    q.max_choices !== undefined ? q.max_choices : null,
    packCorrect(question),
  ];
}
function unpackQuestion(data: any[]): DiSpace.Question {
  if (data == null) return null;
  const q = {
    id: data[0],
    theme_id: data[1],
    title: data[2],
    prompt: data[3],
    max_score: data[4],
    show_solution: !!data[5],
    type_original: data[6],
    type: data[7],
  } as any;
  if (data[8] !== null) q.is_shuffled = !!data[8];
  if (q.type === 1 || q.type === 3 || q.type === 4) q.modal_feedback = data[9];
  if (data[10] !== null) q.max_choices = data[10];
  if (q.type === 2)
    q.correct = data[11].split("|").map(p => p.split(";"));
  else if (q.type === 4)
    q.correct = data[11].split("|");
  else if (q.type === 5)
    q.correct = data[11].split("|").map(p => {
      const [pattern, score] = p.split("/");
      return { pattern: unpackString(pattern), score: +score };
    });

  return q;
}
function diffQuestion(a: DiSpace.Question, b: DiSpace.Question): Diff[] {
  let diffs: Diff[] = [];
  if (a.title !== b.title) diffs.push({ id: b.id, field: 2, old_value: a.title });
  if (a.prompt !== b.prompt) diffs.push({ id: b.id, field: 3, old_value: a.prompt });
  if (a.max_score !== b.max_score && a.max_score != null && b.max_score != null) diffs.push({ id: b.id, field: 4, old_value: a.max_score });
  if (a.show_solution !== b.show_solution) diffs.push({ id: b.id, field: 5, old_value: a.show_solution ? 1 : 0 });
  if (a.type_original !== b.type_original) diffs.push({ id: b.id, field: 6, old_value: a.type_original });
  if (a.type !== b.type) diffs.push({ id: b.id, field: 7, old_value: a.type });
  const aa = a as any;
  const ba = b as any;
  if (aa.is_shuffled !== ba.is_shuffled) diffs.push({ id: b.id, field: 8, old_value: aa.is_shuffled });
  if (aa.modal_feedback !== ba.modal_feedback) diffs.push({ id: b.id, field: 9, old_value: aa.modal_feedback });
  if (aa.max_choices !== ba.max_choices) diffs.push({ id: b.id, field: 10, old_value: aa.max_choices });
  const [aC, bC] = [packCorrect(aa), packCorrect(ba)];
  if (aC !== bC) diffs.push({ id: b.id, field: 11, old_value: aC });
  return diffs;
}

const [optionParameters, optionPlaceholders] = makeParameters([
  "question_id",
  "hash",
  "id",
  "text",
  "score",
  "is_correct",
  "max_matches",
  "mapping",
]);
function packMapping(mapping): string {
  if (mapping == null) return null;
  return mapping.map(m => `${m.hash};${m.score};${m.is_correct ? 1 : 0}`).join("|");
}
function packOption(option: DiSpace.Option): DataRow {
  const o = option as any;
  return [
    option.question_id,
    option.hash,
    option.id,
    option.text,
    o.score !== undefined ? o.score : null,
    o.is_correct !== undefined ? o.is_correct ? 1 : 0 : null,
    o.max_matches !== undefined ? o.max_matches : null,
    o.mapping !== undefined ? packMapping(o.mapping) : null,
  ];
}
function unpackOption(data: any[]): DiSpace.Option {
  if (data == null) return null;
  const o = {
    question_id: data[0],
    hash: data[1],
    id: data[2],
    text: data[3],
  } as any;
  if (data[4] !== null) o.score = data[4];
  if (data[5] !== null) o.is_correct = !!data[5];
  if (data[6] !== null) o.max_matches = data[6];
  if (data[7] !== null)
    o.mapping = data[7].split("|").map(c => {
      const [hash, score, is_correct] = c.split(";");
      return { hash, score: +score, is_correct: !!(+is_correct) };
    });
    return o;
}
function diffOption(a: DiSpace.Option, b: DiSpace.Option): Diff[] {
  let diffs: Diff[] = [];
  if (a.text !== b.text) diffs.push({ id: b.hash, field: 3, old_value: a.text });
  const aa = a as any;
  const ba = b as any;
  if (aa.score !== ba.score && Math.abs(aa.score - ba.score) > 0.015) diffs.push({ id: b.hash, field: 4, old_value: aa.score });
  if (aa.is_correct !== ba.is_correct) diffs.push({ id: b.hash, field: 5, old_value: aa.is_correct ? 1 : 0 });
  if (aa.max_matches !== ba.max_matches) diffs.push({ id: b.hash, field: 6, old_value: aa.max_matches });
  const [aM, bM] = [packMapping(aa.mapping), packMapping(ba.mapping)];
  if (aM !== bM) diffs.push({ id: b.hash, field: 7, old_value: aM });
  return diffs;
}

const [attemptParameters, attemptPlaceholders] = makeParameters([
  "id",
  "test_id",
  "user_id",
  "started_at",
  "finished_at",
  "score",
  "max_score",
  "show_results_mode",
  "open_question_count",
  "is_trial",
  "set_as_read",
  "revision_number",
]);
function packAttempt(attempt: DiSpace.Attempt): DataRow {
  return [
    attempt.id,
    attempt.test_id,
    attempt.user_id,
    attempt.started_at,
    attempt.finished_at,
    attempt.score,
    attempt.max_score,
    attempt.show_results_mode,
    attempt.open_question_count,
    attempt.is_trial ? 1 : 0,
    attempt.set_as_read ? 1 : 0,
    attempt.revision_number,
  ];
}
function unpackAttempt(data: any[]): DiSpace.Attempt {
  if (data == null) return null;
  return {
    id: data[0],
    test_id: data[1],
    user_id: data[2],
    started_at: data[3],
    finished_at: data[4],
    score: data[5],
    max_score: data[6],
    show_results_mode: data[7],
    open_question_count: data[8],
    is_trial: !!data[9],
    set_as_read: !!data[10],
    revision_number: data[11],
    units: undefined,
  };
}
function diffAttempt(a: DiSpace.Attempt, b: DiSpace.Attempt): Diff[] {
  let diffs: Diff[] = [];
  if (a.finished_at !== b.finished_at) diffs.push({ id: b.id, field: 4, old_value: a.finished_at });
  if (a.score !== b.score) diffs.push({ id: b.id, field: 5, old_value: a.score });
  if (a.max_score !== b.max_score) diffs.push({ id: b.id, field: 6, old_value: a.max_score });
  if (a.show_results_mode !== b.show_results_mode) diffs.push({ id: b.id, field: 7, old_value: a.show_results_mode });
  if (a.open_question_count !== b.open_question_count) diffs.push({ id: b.id, field: 8, old_value: a.open_question_count });
  if (a.is_trial !== b.is_trial) diffs.push({ id: b.id, field: 9, old_value: a.is_trial ? 1 : 0 });
  if (a.set_as_read !== b.set_as_read) diffs.push({ id: b.id, field: 10, old_value: a.set_as_read ? 1 : 0 });
  if (a.revision_number !== b.revision_number) diffs.push({ id: b.id, field: 11, old_value: a.revision_number });
  return diffs;
}

const [unitResultParameters, unitResultPlaceholders] = makeParameters([
  "attempt_id",
  "unit_id",
  "score",
  "max_score",
]);
function packUnitResult(unit_result: DiSpace.UnitResult): DataRow {
  return [
    unit_result.attempt_id,
    unit_result.unit_id,
    unit_result.score,
    unit_result.max_score,
  ];
}
function unpackUnitResult(data: any[]): DiSpace.UnitResult {
  if (data == null) return null;
  return {
    attempt_id: data[0],
    unit_id: data[1],
    score: data[2],
    max_score: data[3],
    themes: undefined,
  };
}

const [themeResultParameters, themeResultPlaceholders] = makeParameters([
  "attempt_id",
  "theme_id",
  "score",
  "max_score",
]);
function packThemeResult(theme_result: DiSpace.ThemeResult): DataRow {
  return [
    theme_result.attempt_id,
    theme_result.theme_id,
    theme_result.score,
    theme_result.max_score,
  ];
}
function unpackThemeResult(data: any[]): DiSpace.ThemeResult {
  if (data == null) return null;
  return {
    attempt_id: data[0],
    theme_id: data[1],
    score: data[2],
    max_score: data[3],
    answers: undefined,
  };
}

const [answerParameters, answerPlaceholders] = makeParameters([
  "attempt_id",
  "question_id",
  "score",
  "type",
  "response",
]);
function packAnswer(answer: DiSpace.Answer): DataRow {
  const a = answer as any;
  const response =
    answer.type === 1 || answer.type === 4 ? a.response.length == 1 ? a.response[0] : a.response.join("|")
    : answer.type === 2 || answer.type === 3 ? a.response.map(p => p.join(";")).join("|")
    : /* answer.type === 5 || answer.type === 6 ? */ a.response;
  return [
    answer.attempt_id,
    answer.question_id,
    answer.score,
    answer.type,
    response,
  ];
}
function unpackAnswer(data: any[]): DiSpace.Answer {
  if (data == null) return null;
  const a = {
    attempt_id: data[0],
    question_id: data[1],
    score: data[2],
    type: data[3],
  } as any;
  if (a.type === 1 || a.type === 4) a.response = typeof data[4] === "number" ? [data[4]] : data[4].split("|").map(d => +d || d);
  else if (a.type === 2 || a.type === 3) a.response = data[4].split("|").map(p => p.split(";"));
  else if (a.type === 5 || a.type === 6) a.response = data[4];
  return a;
}

export const Pars = {
  test: testParameters,
  unit: unitParameters,
  theme: themeParameters,
  question: questionParameters,
  option: optionParameters,
  attempt: attemptParameters,
  unitResult: unitResultParameters,
  themeResult: themeResultParameters,
  answer: answerParameters,
}
export const Plac = {
  test: testPlaceholders,
  unit: unitPlaceholders,
  theme: themePlaceholders,
  question: questionPlaceholders,
  option: optionPlaceholders,
  attempt: attemptPlaceholders,
  unitResult: unitResultPlaceholders,
  themeResult: themeResultPlaceholders,
  answer: answerPlaceholders,
}
export const Pack = {
  test: packTest,
  unit: packUnit,
  theme: packTheme,
  question: packQuestion,
  option: packOption,
  attempt: packAttempt,
  unitResult: packUnitResult,
  themeResult: packThemeResult,
  answer: packAnswer,
}
export const Unpack = {
  test: unpackTest,
  unit: unpackUnit,
  theme: unpackTheme,
  question: unpackQuestion,
  option: unpackOption,
  attempt: unpackAttempt,
  unitResult: unpackUnitResult,
  themeResult: unpackThemeResult,
  answer: unpackAnswer,
}
export const Diff = {
  test: diffTest,
  unit: diffUnit,
  theme: diffTheme,
  question: diffQuestion,
  option: diffOption,
  attempt: diffAttempt,
}
