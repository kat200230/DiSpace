import * as DiSpaceRaw from './DiSpace_Raw';
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
  else throw new Error(`Could not resolve ${a} as a Boolean!`);
}
function assert_equal(a: number, b: number, formatter: (a: number, b: number) => string) {
  if (a !== b) throw new Error(formatter(a, b));
}

export function convert_attempt(attempt: DiSpaceRaw.Attempt): DiSpace.Attempt {
  const simple = attempt.simple;
  const res_array = attempt.res_array;
  const unitIds = Object.keys(res_array).filter(i => i !== "final_result");

  // assert_equal(+simple.score, res_array.final_result.score);
  // assert_equal(+simple.max_score, res_array.final_result.max_score);
  // it seems that old units were wiped, and final_result gives incorrect results

  return {
    id: +simple.id,
    test_id: +simple.test_id,
    user_id: +simple.user_id,
    started_at: simple.test_start_time,
    finished_at: simple.test_end_time,
    score: +simple.score,
    max_score: +simple.max_score,
    show_results_mode: simple.show_results,
    open_question_count: +simple.num_of_opened_questions,
    is_trial: to_bool(simple.trial),
    set_as_read: to_bool(simple.set_as_read),
    revision_number: +simple.revision_number,
    units: unitIds.map(id => {
      const unitRaw = res_array[id] as DiSpaceRaw.UnitResults;
      const unit = convert_unit(unitRaw);
      assert_equal(unit.unit_id, +unitRaw.didact.id,
        (a, b) => `Unit id is inconsistent: ${a} !== ${b}.`);
      return unit;
    }),
  }
}
export function convert_unit(unit: DiSpaceRaw.UnitResults): DiSpace.UnitResults {
  const didact = unit.didact;
  const themeIds = Object.keys(unit).filter(i => i !== "didact");

  return {
    unit_id: +didact.id,
    unit_hash: didact.identifier,
    name: didact.name,
    description: didact.description,
    is_visible: to_bool(didact.visible),
    is_shuffled: to_bool(didact.shuffle),
    selection: +didact.selection,
    score: didact._score,
    max_score: didact._max_score,
    themes: themeIds.map(i => {
      const themeRaw = unit[i] as DiSpaceRaw.ThemeResults;
      const theme = convert_theme(themeRaw);
      assert_equal(theme.theme_id, +themeRaw.param.id,
        (a, b) => `Theme id is inconsistent: ${a} !== ${b}.`);
      return theme;
    }),
  }
}
export function convert_theme(theme: DiSpaceRaw.ThemeResults): DiSpace.ThemeResults {
  const param = theme.param;
  const questionIds = Object.keys(theme).filter(i => i !== "param" && i !== "result");

  return {
    theme_id: +param.id,
    theme_hash: param.identifier,
    name: param.name,
    description: param.description,
    is_visible: to_bool(param.visible),
    is_shuffled: to_bool(param.shuffle),
    selection: +param.selection,
    score: theme.result.score,
    max_score: theme.result.max_score,
    answers: questionIds.map(i => {
      const answerRaw = theme[i] as DiSpaceRaw.Answer;
      const answer = convert_answer(answerRaw);
      assert_equal(answer.question_id, +answerRaw.item.id,
        (a, b) => `Question id is inconsistent: ${a} !== ${b}.`);
      return answer;
    }),
  }
}

export function convert_answer(answer: DiSpaceRaw.Answer): DiSpace.Answer {
  const item = answer.item;
  const type_old = +item.type;

  const general: DiSpace.Answer = {
    question_id: +item.id,
    html: answer.view,
    title: item.title,
    prompt: item.prompt,
    score: answer.responses[2],
    max_score: answer.responses[3],
    show_solution: to_bool(item.showSolution),
    type_old: type_old,
    type: undefined,
  };

  if (type_old === 1 || type_old === 2) {
    general.type = 1;
    return convert_simple_answer(general, item as DiSpaceRaw.SimpleAnswerInfo, answer);
  }
  else if (type_old === 3) {
    general.type = 2;
    return convert_pair_answer(general, item as DiSpaceRaw.PairAnswerInfo, answer);
  }
  else if (type_old === 4) {
    general.type = 3;
    return convert_associative_answer(general, item as DiSpaceRaw.AssociativeAnswerInfo, answer);
  }
  else if (type_old === 6) {
    general.type = 4;
    return convert_order_answer(general, item as DiSpaceRaw.OrderAnswerInfo, answer);
  }
  else if (type_old === 5 || type_old === 7) {
    general.type = 5;
    return convert_custom_input_answer(general, item as DiSpaceRaw.CustomInputAnswerInfo, answer);
  }
  else if (type_old === 8) {
    general.type = 6;
    return convert_open_question_answer(general, item as DiSpaceRaw.OpenQuestionAnswerInfo, answer);
  }
  else throw new Error(`Unknown question type_old ${type_old}!`);
}
export function convert_simple_answer(general: DiSpace.Answer, item: DiSpaceRaw.SimpleAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.SimpleAnswer {
  if (general.type_old === 1) assert_equal(+item.maxChoices, 1,
    (a, b) => `Answer ${item.id} (type 1) has invalid .maxChoices value: ${a} !== ${b}.`);
  assert_equal(general.max_score, +item.upperBound,
    (a, b) => `Answer ${item.id}'s .max_score/.upperBound is inconsistent: ${a} !== ${b}.`);

  const optionHashes = Object.keys(item.simpleChoice);
  const optionIds = answer.responses[4].split(",");
  
  const correctHashes = Array.isArray(item.correctResponse) ? item.correctResponse : [item.correctResponse];

  let mapping = item.mapping;
  if (!mapping) { // polyfill
    const count = Array.isArray(item.correctResponse) ? item.correctResponse.length : 1;
    const delta = Math.round(general.max_score / count * 100) / 100;
    let remainingPoints = general.max_score;

    mapping = {};
    for (let optionHash of optionHashes) {
      if (correctHashes.includes(optionHash)) {
        remainingPoints -= delta;
        let myScore = delta;
        if (remainingPoints < 0) myScore += remainingPoints;
        mapping[optionHash] = myScore;
      }
      else mapping[optionHash] = -delta;
    }
    if (remainingPoints > 0) mapping[correctHashes.at(-1)] += remainingPoints;
  }
  const responseHashes = item.response ? Object.keys(item.response) : [];

  return {
    ...general,

    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,
    max_choices: +item.maxChoices,

    choices: optionHashes.map((hash, ind): DiSpace.SimpleChoice => ({
      option_id: +optionIds[ind],
      option_hash: hash,
      text: item.simpleChoice[hash],
      is_correct: correctHashes.includes(hash),
      score: mapping[hash],
    })),

    response: responseHashes.map(h => optionHashes.indexOf(h)),
  };
}
export function convert_pair_answer(general: DiSpace.Answer, item: DiSpaceRaw.PairAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.PairAnswer {
  assert_equal(general.max_score, +item.upperBound,
    (a, b) => `Answer ${item.id}'s .max_score/.upperBound is inconsistent: ${a} !== ${b}.`);

  const optionHashes = Object.keys(item.simpleAssociableChoice);
  const optionIds = answer.responses[4].split(" ");

  const responsePairs = !Array.isArray(item.response) ? Object.keys(item.response) : [];

  return {
    ...general,

    is_shuffled: to_bool(item.shuffle),

    choices: optionHashes.map((hash, ind): DiSpace.PairChoice => {
      const val = item.simpleAssociableChoice[hash];
      return {
        option_id: +optionIds[ind],
        option_hash: hash,
        text: val.val,
        max_matches: +val.matchMax,
      };
    }),

    response: responsePairs.filter(p => item.response[p]).map(p => {
      let vals = p.split(" ");
      return [optionHashes.indexOf(vals[0]), optionHashes.indexOf(vals[1])];
    }),
    correct: item.correctResponse.map(p => {
      let vals = p.split(" ");
      return [optionHashes.indexOf(vals[0]), optionHashes.indexOf(vals[1])];
    }),
  };
}
export function convert_associative_answer(general: DiSpace.Answer, item: DiSpaceRaw.AssociativeAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.AssociativeAnswer {
  assert_equal(general.max_score, +item.upperBound,
    (a, b) => `Answer ${item.id}'s .max_score/.upperBound is inconsistent: ${a} !== ${b}.`);

  const colHashes = Object.keys(item.simpleAssociableChoice.cols);
  const rowHashes = Object.keys(item.simpleAssociableChoice.rows);
  const [colIds, rowIds] = answer.responses[4].split(",").map(ids => ids.split(" "));

  let mapping = item.mapping;
  if (!mapping) { // polyfill
    throw new Error("Not implemented.");
  }

  const mappingKeys = Object.keys(item.mapping);
  const responsePairs = Object.keys(item.response);

  return {
    ...general,

    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,

    columns: colHashes.map((hash, ind) => ({
      option_id: colIds ? +colIds[ind] : -1,
      option_hash: hash,
      text: item.simpleAssociableChoice.cols[hash],
    })),
    rows: rowHashes.map((hash, ind): DiSpace.AssociativeRow => {
      const responsePair = responsePairs.find(p => p.startsWith(hash));
      
      return {
        option_id: rowIds ? +rowIds[ind] : -1,
        option_hash: hash,
        text: item.simpleAssociableChoice.rows[hash],

        mapping: mappingKeys.filter(p => p.startsWith(hash)).map(p => {
          let other = p.split(" ")[1];
          return {
            index: colHashes.indexOf(other),
            score: mapping[p],
            is_correct: item.correctResponse.includes(p),
          };
        }),

        response: responsePair && item.response[responsePair] ? colHashes.indexOf(responsePair.split(" ")[1]) : -1,
      };
    }),
  };
}
export function convert_order_answer(general: DiSpace.Answer, item: DiSpaceRaw.OrderAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.OrderAnswer {
  assert_equal(general.max_score, +item.upperBound,
    (a, b) => `Answer ${item.id}'s .max_score/.upperBound is inconsistent: ${a} !== ${b}.`);

  const optionHashes = Object.keys(item.simpleChoice);
  const optionIds = answer.responses[4].split(" ");

  return {
    ...general,

    is_shuffled: to_bool(item.shuffle),
    modal_feedback: item.modalFeedback || null,

    choices: item.correctResponse.map(hash => ({
      option_id: +optionIds[optionHashes.indexOf(hash)],
      option_hash: hash,
      text: item.simpleChoice[hash],
    })),

    response: Object.keys(item.response).map(hash => item.correctResponse.indexOf(hash)),
  };
}
export function convert_custom_input_answer(general: DiSpace.Answer, item: DiSpaceRaw.CustomInputAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.CustomInputAnswer {
  assert_equal(general.max_score, +item.upperBound,
    (a, b) => `Answer ${item.id}'s .max_score/.upperBound is inconsistent: ${a} !== ${b}.`);

  return {
    ...general,

    correct: Array.isArray(item.correctResponse)
      ? item.correctResponse.map((pattern, i) => ({ pattern, score: item.mapping[i] }))
      : [{ pattern: item.correctResponse, score: general.max_score }],
    response: item.response,
  };
}
export function convert_open_question_answer(general: DiSpace.Answer, item: DiSpaceRaw.OpenQuestionAnswerInfo, answer: DiSpaceRaw.Answer): DiSpace.OpenQuestionAnswer {
  
  return {
    ...general,

    response: item.response,
  };
}
