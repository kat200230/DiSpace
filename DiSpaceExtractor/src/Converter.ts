import * as Raw from './DiSpace_Raw';
import * as DiSpace from './DiSpace';

function to_bool(a: boolean | number | string): boolean {
  if (a === true) return true;
  else if (a === false) return false;
  else if (a === 1) return true;
  else if (a === 0) return false;
  else if (a === "1") return true;
  else if (a === "0") return false;
  else if (a === "true") return true;
  else if (a === "false") return false;
  else if (+a === 1) return true;
  else if (+a === 0) return false;
  else if (a === undefined) return false;
  else throw new Error(`Could not resolve ${a} as a Boolean!`);
}
function to_unix(date: string): number {
  if (date === null) return null;
  return Math.round(new Date(date).getTime() / 1000);
}
function assert_equal(a: any, b: any, formatter: (a: any, b: any) => string): void {
  if (a !== b) {
    throw new Error(formatter(a, b));
  }
}
function compare(a: string, b: string);
function compare(a: number, b: number);
function compare(a: string | number, b: string | number) {
  if (a < b) return -1;
  else if (a > b) return 1;
  else return 0;
}

export function convert_attempt(a: Raw.Attempt): [DiSpace.Test, DiSpace.Attempt] {
  const simple = a.simple;
  const res_array = a.res_array;

  const units: DiSpace.Unit[] = [];
  const unit_results: DiSpace.UnitResult[] = [];

  const test: DiSpace.Test = {
    id: +simple.test_id,
    units: units,
  };
  const attempt: DiSpace.Attempt = {
    id: +simple.id,
    test_id: test.id,
    user_id: +simple.user_id,
    started_at: to_unix(simple.test_start_time),
    finished_at: to_unix(simple.test_end_time),
    score: +simple.score,
    max_score: +simple.max_score,
    show_results_mode: simple.show_results,
    open_question_count: +simple.num_of_opened_questions,
    is_trial: to_bool(simple.trial),
    set_as_read: to_bool(simple.set_as_read),
    revision_number: +simple.revision_number,
    units: unit_results,
  };

  const unit_ids = Object.keys(res_array).filter(i => i !== "final_result");
  unit_ids.map(i => res_array[i] as Raw.UnitResult).forEach(u => {
    const [unit, unit_result] = convert_unit(u, attempt.id, test.id);
    units.push(unit);
    unit_results.push(unit_result);
  });

  return [test, attempt];
}
export function convert_unit(u: Raw.UnitResult, attempt_id: number, test_id: number): [DiSpace.Unit, DiSpace.UnitResult] {
  const didact = u.didact;

  const themes: DiSpace.Theme[] = [];
  const theme_results: DiSpace.ThemeResult[] = [];

  const unit: DiSpace.Unit = {
    id: +didact.id,
    test_id: test_id,
    hash: didact.identifier.split("_")[1],
    name: didact.name,
    description: didact.description,
    selection: +didact.selection,
    is_visible: to_bool(didact.visible),
    is_shuffled: to_bool(didact.shuffle),
    themes: themes,
  };
  const unit_result: DiSpace.UnitResult = {
    attempt_id: attempt_id,
    unit_id: unit.id,
    score: +didact._score,
    max_score: +didact._max_score,
    themes: theme_results,
  };

  const theme_ids = Object.keys(u).filter(i => i !== "didact");
  theme_ids.map(i => u[i] as Raw.ThemeResult).forEach(t => {
    const [theme, theme_result] = convert_theme(t, attempt_id, unit.id);
    themes.push(theme);
    theme_results.push(theme_result);
  });

  return [unit, unit_result];
}
export function convert_theme(t: Raw.ThemeResult, attempt_id: number, unit_id: number): [DiSpace.Theme, DiSpace.ThemeResult] {
  const param = t.param;
  const result = t.result;

  const questions: DiSpace.Question[] = [];
  const answers: DiSpace.Answer[] = [];

  const theme: DiSpace.Theme = {
    id: +param.id,
    unit_id: unit_id,
    hash: param.identifier.split("_")[1],
    name: param.name,
    description: param.description,
    selection: +param.selection,
    is_visible: to_bool(param.visible),
    is_shuffled: to_bool(param.shuffle),
    questions: questions,
  };
  const theme_result: DiSpace.ThemeResult = {
    attempt_id: attempt_id,
    theme_id: theme.id,
    score: +result.score,
    max_score: +result.max_score,
    answers: answers,
  };
  
  const question_ids = Object.keys(t).filter(i => i !== "param" && i !== "result");
  question_ids.map(i => t[i] as Raw.Answer).forEach(a => {
    const [question, answer] = convert_answer(a, attempt_id, theme.id);
    questions.push(question);
    answers.push(answer);
  });

  return [theme, theme_result];
}
export function convert_answer(a: Raw.Answer, attempt_id: number, theme_id: number): [DiSpace.Question, DiSpace.Answer] {
  const item = a.item;
  const type_original = +item.type;

  const gen_question: DiSpace.Question = {
    id: +item.id,
    theme_id: theme_id,
    title: item.title,
    prompt: item.prompt,
    max_score: +a.responses[3],
    show_solution: to_bool(item.showSolution),
    type_original: type_original,
    type: undefined,
  };
  const gen_answer: DiSpace.Answer = {
    attempt_id: attempt_id,
    question_id: gen_question.id,
    score: +a.responses[2],
    type: gen_question.type,
  };

  if (type_original == 1 || type_original == 2) {
    gen_question.type = gen_answer.type = 1;
    return convert_simple_answer(a, gen_question, gen_answer);
  }
  else if (type_original == 3) {
    gen_question.type = gen_answer.type = 2;
    return convert_pair_answer(a, gen_question, gen_answer);
  }
  else if (type_original == 4) {
    gen_question.type = gen_answer.type = 3;
    return convert_associative_answer(a, gen_question, gen_answer);
  }
  else if (type_original == 6) {
    gen_question.type = gen_answer.type = 4;
    return convert_order_answer(a, gen_question, gen_answer);
  }
  else if (type_original == 5 || type_original == 7) {
    gen_question.type = gen_answer.type = 5;
    return convert_custom_input_answer(a, gen_question, gen_answer);
  }
  else if (type_original == 8) {
    gen_question.type = gen_answer.type = 6;
    return convert_open_question_answer(a, gen_question, gen_answer);
  }
  else throw new Error(`Unknown question type: ${type_original}!`);

}
export function convert_simple_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.SimpleQuestion, DiSpace.SimpleAnswer] {
  const item = a.item as Raw.SimpleAnswerInfo;

  // if (gen_question.type_original === 1) assert_equal(+item.maxChoices, 1,
  //   (a, b) => `Answer ${item.id} (type 1) has invalid .maxChoices value: ${a} !== ${b}.`);

  const options : DiSpace.SimpleOption[] = [];

  const question: DiSpace.SimpleQuestion = {
    ...gen_question,
    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,
    max_choices: gen_question.type_original === 1 ? 1 : +item.maxChoices,
    options: options,
  };

  const optionHashes = Object.keys(item.simpleChoice);
  const responseHashes = item.response ? Object.keys(item.response) : [];
  const optionIds = a.responses[4].split(",");
  
  const correctHashes = Array.isArray(item.correctResponse) ? item.correctResponse : [item.correctResponse];

  let mapping = item.mapping;
  if (!mapping) { // polyfill
    const count = correctHashes.length;
    const delta = Math.floor(question.max_score / count * 100) / 100;
    let remainingPoints = question.max_score;

    mapping = {};
    let lastCorrectHash = null;
    for (let optionHash of optionHashes) {
      if (correctHashes.includes(optionHash)) {
        remainingPoints -= delta;
        mapping[optionHash] = delta;
        lastCorrectHash = optionHash;
      }
      else mapping[optionHash] = -delta;
    }
    if (remainingPoints != 0) mapping[lastCorrectHash] += remainingPoints;
  }

  optionHashes.forEach((hash, ind) => {
    options.push({
      hash: hash.startsWith("Choice_") ? hash.split("_")[1] : hash,
      id: optionIds[ind] ? +optionIds[ind] : null,
      question_id: question.id,
      text: item.simpleChoice[hash],
      score: Math.round(mapping[hash] * 100) / 100,
      is_correct: correctHashes.includes(hash),
    });
  });

  const answer: DiSpace.SimpleAnswer = {
    ...gen_answer,
    response: responseHashes.map(h => {
      let id: number | null = null;
      try { id = +optionIds[optionHashes.indexOf(h)]; } catch (ex) { }
      if (id) return id;
      return h.startsWith("Choice_") ? h.split("_")[1] : h;
    }),
  };

  return [question, answer];
}
export function convert_pair_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.PairQuestion, DiSpace.PairAnswer] {
  const item = a.item as Raw.PairAnswerInfo;

  const options: DiSpace.PairOption[] = [];

  const optionHashes = Object.keys(item.simpleAssociableChoice);
  const optionIds = a.responses[4].split(" ");

  const responsePairs = !Array.isArray(item.response) ? Object.keys(item.response) : [];

  const question: DiSpace.PairQuestion = {
    ...gen_question,
    is_shuffled: to_bool(item.shuffle),
    options: options,
    correct: item.correctResponse.map(p => p.split(" ") as [string, string]),
  };

  optionHashes.forEach((hash, ind) => {
    const val = item.simpleAssociableChoice[hash];
    options.push({
      hash: hash.startsWith("Choice_") ? hash.split("_")[1] : hash,
      id: optionIds[ind] ? +optionIds[ind] : null,
      question_id: question.id,
      text: val.val,
      max_matches: +val.matchMax,
    });
  });

  const answer: DiSpace.PairAnswer = {
    ...gen_answer,
    response: responsePairs.filter(p => item.response[p]).map(p => p.split(" ")
      .map(s => s.startsWith("Choice_") ? s.split("_")[1] : s) as [string, string]),
  };

  return [question, answer];
}
export function convert_associative_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.AssociativeQuestion, DiSpace.AssociativeAnswer] {
  const item = a.item as Raw.AssociativeAnswerInfo;

  let sim = Array.isArray(item.simpleAssociableChoice) ? {cols:{},rows:{}} : item.simpleAssociableChoice;
  const colHashes = Object.keys(sim.cols ?? {});
  const rowHashes = Object.keys(sim.rows ?? {});
  const [colIds, rowIds] = a.responses[4].split(",").map(ids => ids.split(" "));

  let mapping = item.mapping;
  if (!mapping) { // polyfill
    const count = rowHashes.length;
    const delta = Math.floor(gen_question.max_score / count * 100) / 100;
    let remainingPoints = gen_question.max_score;

    mapping = {};
    let lastCorrectPair = null;
    for (let rowHash of rowHashes) {
      for (let colHash of colHashes) {
        let pair = `${rowHash} ${colHash}`;
        if (item.correctResponse.includes(pair)) {
          remainingPoints -= delta;
          mapping[pair] = delta;
          lastCorrectPair = pair;
        }
        else mapping[pair] = -delta;
      }
    }
    if (remainingPoints != 0) mapping[lastCorrectPair] += remainingPoints;
  }

  const mappingKeys = Object.keys(mapping);
  const responsePairs = Array.isArray(item.response) ? [] : Object.keys(item.response);

  const question: DiSpace.AssociativeQuestion = {
    ...gen_question,

    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,

    columns: colHashes.map((hash, ind) => ({
      id: colIds ? +colIds[ind] : -1,
      question_id: gen_question.id,
      hash: hash.startsWith("Choice_") ? hash.split("_")[1] : hash,
      text: item.simpleAssociableChoice.cols[hash],
    })),
    rows: rowHashes.map((hash, ind): DiSpace.AssociativeRow => ({
      id: rowIds ? +rowIds[ind] : -1,
      question_id: gen_question.id,
      hash: hash.startsWith("Choice_") ? hash.split("_")[1] : hash,
      text: item.simpleAssociableChoice.rows[hash],

      mapping: mappingKeys.filter(p => p.startsWith(hash)).map(p => {
        let other = p.split(" ")[1];
        return {
          hash: other.startsWith("Choice_") ? other.split("_")[1] : other,
          score: Math.round(mapping[p] * 100) / 100,
          is_correct: (item.correctResponse || []).includes(p),
        };
      }).sort((a, b) => compare(a.hash, b.hash)),
    })),

    correct: (item.correctResponse || []).map(p => p.split(" ").map(s => s.startsWith("Choice_") ? s.split("_")[1] : s) as [string, string]),
  };
  const answer: DiSpace.AssociativeAnswer = {
    ...gen_answer,
    
    response: responsePairs.map(p => p.split(" ").map(s => s.startsWith("Choice_") ? s.split("_")[1] : s) as [string, string]),
  };

  return [question, answer];
}
export function convert_order_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.OrderQuestion, DiSpace.OrderAnswer] {
  const item = a.item as Raw.OrderAnswerInfo;

  const optionHashes = Object.keys(item.simpleChoice);
  const optionIds = a.responses[4].split(" ");

  const question: DiSpace.OrderQuestion = {
    ...gen_question,

    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,

    options: item.correctResponse.map(hash => ({
      id: +optionIds[optionHashes.indexOf(hash)],
      question_id: gen_question.id,
      hash: hash.startsWith("Choice_") ? hash.split("_")[1] : hash,
      text: item.simpleChoice[hash] ?? ":unknown:",
    })),

    correct: item.correctResponse,
  };
  const answer: DiSpace.OrderAnswer = {
    ...gen_answer,

    response: Object.keys(item.response).map(h => {
      let id: number | null = null;
      try { id = +optionIds[optionHashes.indexOf(h)]; } catch (ex) { }
      if (id) return id;
      return h.startsWith("Choice_") ? h.split("_")[1] : h;
    }),
  };

  return [question, answer];
}
export function convert_custom_input_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.CustomInputQuestion, DiSpace.CustomInputAnswer] {
  const item = a.item as Raw.CustomInputAnswerInfo;

  const question: DiSpace.CustomInputQuestion = {
    ...gen_question,
    
    correct: Array.isArray(item.correctResponse)
      ? item.correctResponse.map((pattern, i) => ({ pattern, score: item.mapping[i] }))
      : [{ pattern: item.correctResponse, score: gen_question.max_score }],
  };
  const answer: DiSpace.CustomInputAnswer = {
    ...gen_answer,

    response: Array.isArray(item.response) ? "" : item.response || "",
  };

  return [question, answer];
}
export function convert_open_question_answer(a: Raw.Answer, gen_question: DiSpace.Question, gen_answer: DiSpace.Answer): [DiSpace.OpenQuestion, DiSpace.OpenQuestionAnswer] {
  const item = a.item as Raw.CustomInputAnswerInfo;

  const answer: DiSpace.CustomInputAnswer = {
    ...gen_answer,
    response: Array.isArray(item.response) ? "" : item.response || "",
  };
  if (gen_answer.score == 0 && gen_question.max_score == 0) gen_question.max_score = null;
  // max_score is not known to this particular answer

  return [gen_question, answer];
}

export function split_up_more(data: [DiSpace.Test, DiSpace.Attempt][]): [DiSpace.Test[], DiSpace.Attempt[], DiSpace.Unit[], DiSpace.UnitResult[], DiSpace.Theme[], DiSpace.ThemeResult[], DiSpace.Question[], DiSpace.Answer[], DiSpace.Option[]] {
  let tests = data.map(d => d[0]);
  let attempts = data.map(d => d[1]);
  let units = tests.flatMap(t => t.units);
  let unit_results = attempts.flatMap(a => a.units);
  let themes = units.flatMap(u => u.themes);
  let theme_results = unit_results.flatMap(u => u.themes);
  let questions = themes.flatMap(t => t.questions);
  let answers = theme_results.flatMap(t => t.answers);
  let options = questions.flatMap(q => {
    if (q.type === 1 || q.type === 2 || q.type === 4)
      return (q as any).options;
    else if (q.type === 3) {
      return (q as any).rows.concat((q as any).columns);
    }
  });

  return [
    tests,
    attempts,
    units,
    unit_results,
    themes,
    theme_results,
    questions,
    answers,
    options,
  ];
}
export function split_up(test: DiSpace.Test, attempt: DiSpace.Attempt): [DiSpace.Unit[], DiSpace.UnitResult[], DiSpace.Theme[], DiSpace.ThemeResult[], DiSpace.Question[], DiSpace.Answer[], DiSpace.Option[]] {
  let units = test.units;
  let unit_results = attempt.units;
  let themes = units.flatMap(u => u.themes);
  let theme_results = unit_results.flatMap(u => u.themes);
  let questions = themes.flatMap(t => t.questions);
  let answers = theme_results.flatMap(t => t.answers);
  let options = questions.flatMap(q => {
    if (q.type === 1 || q.type === 2 || q.type === 4)
      return (q as any).options;
    else if (q.type === 3) {
      return (q as any).rows.concat((q as any).columns);
    }
    else return [];
  });

  return [
    units,
    unit_results,
    themes,
    theme_results,
    questions,
    answers,
    options,
  ];
}
