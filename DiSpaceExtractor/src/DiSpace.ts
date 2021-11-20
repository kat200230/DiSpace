export type Attempt = {
  id: number,
  test_id: number,
  user_id: number,
  started_at: string,
  finished_at: string,
  score: number,
  max_score: number,
  show_results_mode: string,
  open_question_count: number,
  is_trial: boolean,
  set_as_read: boolean,
  revision_number: number,
  units: UnitResults[],
}
export type UnitResults = {
  unit_id: number,
  unit_hash: string,
  name: string | null,
  description: string | null,
  is_visible: boolean,
  is_shuffled: boolean,
  selection: number,
  score: number,
  max_score: number,
  themes: ThemeResults[],
}
export type ThemeResults = {
  theme_id: number,
  theme_hash: string,
  name: string | null,
  description: string | null,
  is_visible: boolean,
  is_shuffled: boolean,
  selection: number,
  score: number,
  max_score: number,
  answers: Answer[],
}
export interface Answer {
  question_id: number,
  html: string,
  title: string,
  prompt: string,
  score: number,
  max_score: number,
  show_solution: boolean,
  type_old: number,
  type: number,
}
export interface Choice {
  option_id: number,
  option_hash: string,
  text: string,
}

export type SimpleAnswer = Answer & {
  // type_old: 1 | 2,
  // type: 1,
  is_shuffled: boolean,
  modal_feedback: string | null,
  max_choices: number, // 0 ~ ∞
  choices: SimpleChoice[],
  response: number[], // indices
}
export type SimpleChoice = Choice & {
  score: number,
  is_correct: boolean,
}

export type PairAnswer = Answer & {
  // type_old: 3,
  // type: 2,
  is_shuffled: boolean,
  choices: PairChoice[],
  response: [number, number][], // indices
  correct: [number, number][], // indices
}
export type PairChoice = Choice & {
  max_matches: number,
}

export type AssociativeAnswer = Answer & {
  // type_old: 4,
  // type: 3,
  is_shuffled: boolean,
  modal_feedback: string | null,
  rows: AssociativeRow[],
  columns: Choice[],
}
export type AssociativeRow = Choice & {
  mapping: { index: number, score: number, is_correct: boolean }[],
  response: number, // -1 ~ not selected
}

export type OrderAnswer = Answer & {
  // type_old: 6,
  // type: 4,
  is_shuffled: boolean,
  modal_feedback: string | null,
  choices: Choice[],
  response: number[],
}

export type CustomInputAnswer = Answer & {
  // type_old: 5 | 7,
  // type: 5,
  correct: { pattern: string, score: number }[],
  response: string,
}

export type OpenQuestionAnswer = Answer & {
  // type_old: 8,
  // type: 6,
  response: string,
}
