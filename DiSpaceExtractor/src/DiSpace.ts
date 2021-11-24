import * as SQLite from 'better-sqlite3';

export type Test = {
  id: number, // pk
  units: Unit[],
}
export type Attempt = {
  id: number, // pk
  test_id: number, // index
  user_id: number, // index
  started_at: number,
  finished_at: number | null,
  score: number,
  max_score: number, // unreliable, gives independent results
  show_results_mode: string,
  open_question_count: number,
  is_trial: boolean,
  set_as_read: boolean,
  revision_number: number,
  units: UnitResult[],
}

export type Unit = {
  id: number, // pk
  test_id: number, // index
  hash: string,    // index
  name: string | null,
  description: string | null,
  selection: number,
  is_visible: boolean,
  is_shuffled: boolean,
  themes: Theme[],
}
export type UnitResult = {
  attempt_id: number, // pk
  unit_id: number,    // pk
  // unit_hash: string,
  score: number,
  max_score: number,
  themes: ThemeResult[],
}

export type Theme = {
  id: number, // pk
  unit_id: number, // index
  hash: string,    // index
  name: string | null,
  description: string | null,
  selection: number,
  is_visible: boolean,
  is_shuffled: boolean,
  questions: Question[],
}
export type ThemeResult = {
  attempt_id: number, // pk
  theme_id: number,   // pk
  // theme_hash: string,
  score: number,
  max_score: number,
  answers: Answer[],
}

export interface Question {
  id: number, // pk
  theme_id: number, // index
  title: string,
  prompt: string,
  max_score: number | null, // null, if it wasn't discovered yet, like in case with open questions
  show_solution: boolean,
  type_original: number,
  type: number,
  // Derived properties:
  // is_shuffled: boolean, - all, except for custom input and open
  // modal_feedback: string | null, - simple, associative, order
  // max_choices: number, - simple
  // correct: [string, string][], // ids □;□|□;□|□;□ - pairs
  // correct: string[], // ids □|□|□ - order
  // correct: { pattern: string, score: number }[], // □/□|□/□|□/□ (replace '/' with '&s;' and '|' with '&p;' in pattern) - custom input
}
export interface Answer {
  attempt_id: number,  // pk
  question_id: number, // pk
  // html: string,
  score: number,
  type: number,
  // Derived properties:
  // response: string[], // ids □|□|□ - simple
  // response: [string, string][], // ids □;□|□;□|□;□ - pair
  // response: [string, string][], // ids □;□|□;□|□;□ (row;column) - associative
  // response: string[], // ids □|□|□ - order
  // response: string, - custom input and open
}

export interface Option {
  hash: string, // pk
  id: number | null,   // index
  question_id: number, // index
  text: string,
  // Derived properties:
  // score: number, - simple
  // is_correct: boolean, // prop? - simple
  // max_matches: number, - pair
  // mapping: { hash: string, score: number, is_correct: boolean }[], // □;□;□|□;□;□|□;□;□ - associative row
  // 
}

export type SimpleQuestion = Question & {
  is_shuffled: boolean,
  modal_feedback: string | null,
  max_choices: number,
  options: SimpleOption[],
  // type_original: 1 | 2,
  // type: 1,
}
export type SimpleOption = Option & {
  score: number,
  is_correct: boolean, // prop?
}
export type SimpleAnswer = Answer & {
  response: string[], // ids □|□|□
  // type: 1,
}

export type PairQuestion = Question & {
  is_shuffled: boolean,
  options: PairOption[],
  correct: [string, string][], // ids □;□|□;□|□;□
  // type_original: 3,
  // type: 2,
}
export type PairOption = Option & {
  max_matches: number,
}
export type PairAnswer = Answer & {
  response: [string, string][], // ids □;□|□;□|□;□
  // type: 2,
}

export type AssociativeQuestion = Question & {
  is_shuffled: boolean,
  modal_feedback: string | null,
  rows: AssociativeRow[],
  columns: AssociativeColumn[],
  correct: [string, string][], // ids □;□|□;□|□;□ (row;column)
  // type_original: 4,
  // type: 3,
}
export type AssociativeRow = Option & {
  mapping: { hash: string, score: number, is_correct: boolean }[], // □;□;□|□;□;□|□;□;□
}
export type AssociativeColumn = Option;
export type AssociativeAnswer = Answer & {
  response: [string, string][], // ids □;□|□;□|□;□ (row;column)
  // type: 3,
}

export type OrderQuestion = Question & {
  is_shuffled: boolean,
  modal_feedback: string | null,
  options: OrderOption[],
  correct: string[], // ids □|□|□
  // type_original: 6,
  // type: 4,
}
export type OrderOption = Option;
export type OrderAnswer = Answer & {
  response: string[], // ids □|□|□
  // type: 4,
}

export type CustomInputQuestion = Question & {
  correct: { pattern: string, score: number }[], // □/□|□/□|□/□ (replace '/' with '&s;' and '|' with '&p;' in pattern)
  // type_original: 5 | 7,
  // type: 5,
}
export type CustomInputAnswer = Answer & {
  response: string,
  // type: 5,
}

export type OpenQuestion = Question & {
  // type_original: 8,
  // type: 6,
}
export type OpenQuestionAnswer = Answer & {
  response: string,
  // type: 6,
}
















