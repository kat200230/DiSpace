export type GetAttemptAPIResult = { error: string } | Attempt;

export type Attempt = {
  simple: AttemptSummary,
  res_array: {
    [duId: number]: UnitResult,
    final_result: { score: number, max_score: number },
  },
  menu_str: string, // what the heck is that
}
export type AttemptSummary = {
  id: string,
  test_start_time: string,
  test_end_time: string | null,
  score: string,
  max_score: string, // unreliable
  show_results: string,
  num_of_opened_questions: string,
  trial: string,
  set_as_read: string,
  test_id: string,
  user_id: string,
  revision_number: string | null,
}

export type UnitResult = {
  didact: UnitResultSummary,
  [themeId: number]: ThemeResult,
}
export type UnitResultSummary = ThemeResultSummary & {
  _score: number,
  _max_score: number,
}

export type ThemeResult = {
  [questionId: number]: Answer,
  param: ThemeResultSummary,
  result: { score: number, max_score: number },
}
export type ThemeResultSummary = {
  id: string,
  name: string | null,
  identifier: string,
  test_id: string,
  visible: string,
  parent_id: string,
  shuffle: string,
  selection: string,
  description: string | null,
}

export type Answer = {
  view: string,
  item: AnswerInfo,
  responses: [
    /*[0]*/ response: string | string[],
    /*[1]*/ correct: string | string[],
    /*[2]*/ score: number,
    /*[3]*/ max_score: number,
    /*[4]*/ option_ids: string, // Note: unshuffled, like in .simpleChoice
  ],
}
export interface AnswerInfo {
  type: string,

  title: string,
  prompt: string,
  id: string,
  showSolution: string,
}

export type SimpleAnswerInfo = AnswerInfo & {
  type: "1" | "2",
  response: { [hash: string]: SimpleChoice },
  correctResponse: string | string[],
  mapping?: { [hash: string]: number },
  shuffle: string,
  modalFeedback: string | false,
  upperBound?: number,
  maxChoices: string, // 0 for ∞
  simpleChoice: { [hash: string]: string },
}
export type SimpleChoice = {
  id: string,
  item_id: string,
  global_id: string, // aka. hash
}

export type PairAnswerInfo = AnswerInfo & {
  type: "3",
  response: [] | { [pair: string]: boolean },
  correctResponse: string[],
  mapping?: { [hashPair: string]: number },
  shuffle: string,
  // no modalFeedback
  upperBound: number,
  simpleAssociableChoice: {
    [hash: string]: { val: string, matchMax: string }
  },
}

export type AssociativeAnswerInfo = AnswerInfo & {
  type: "4",
  response: [] | { [pair: string]: boolean },
  correctResponse: string[],
  mapping?: { [hashPair: string]: number },
  shuffle: string,
  modalFeedback: string | false,
  upperBound: number,
  simpleAssociableChoice: {
    cols: { [hash: string]: string },
    rows: { [hash: string]: string },
  },
}

export type OrderAnswerInfo = AnswerInfo & {
  type: "6",
  response: { [hash: string]: OrderChoice },
  shuffle: string,
  modalFeedback: string | false,
  upperBound: number,
  simpleChoice: { [hash: string]: string },
  correctResponse: string[],
}
export type OrderChoice = {
  id: string,
  item_id: string,
  global_id: string, // aka. hash
}

export type CustomInputAnswerInfo = AnswerInfo & {
  type: "5" | "7",
  response: string,
  upperBound: number,
  correctResponse: string | string[],
  mapping?: number[],
}

export type OpenQuestionAnswerInfo = AnswerInfo & {
  type: "8",
  response: string,
}
