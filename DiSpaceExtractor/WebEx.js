function dipeek_get(i){
  return new Promise(r=>$.ajax({
    type:'POST',url:'/ditest/index/result',
    data:{action:'get_results_by_test',score_id:i,test_id:40001},
    dataType:'json',success:d=>r(d),
    error:(x,s,e)=>r({error:`ExtractionError:${e} (${s})`})
  }))}
let _ds,_da;
function dipeek_cancel(){_ds=1}
function __dipeek_cur(){
  if(_da)return _da;let t=sessionStorage._da;
  if(!t)throw new Error("Попытка не была сохранена!");
  return _da=JSON.parse(t)}
function __dipeek_set_cur(a){_da=a;sessionStorage._da=JSON.stringify(a)}

async function dipeek_search(x,r=200){
  _ds=0;let h=await dipeek_get(x);_t=0;_tqs=0;
  if (!h.error&&+h.simple.user_id==+USER_ID){__dipeek_set_cur(h);
    return console.log(`Удалось найти попытку! (${h.simple.id})`)}
  for(let i=1;i<=r;i++){
    if(_ds)return console.log(`Поиск прерван.`);
    h=await dipeek_get(x+i);
    if (!h.error&&+h.simple.user_id==+USER_ID){__dipeek_set_cur(h);
      console.log(`Удалось найти попытку! (${h.simple.id})`);return h}
    if(_ds)return console.log(`Поиск прерван.`);
    h=await dipeek_get(x-i);
    if(!h.error&&+h.simple.user_id==+USER_ID){__dipeek_set_cur(h);
      console.log(`Удалось найти попытку! (${h.simple.id})`);return h}}
  console.error(`Не удалось найти попытки у ИДЫ = ${x}!`)}
async function dipeek_search2(x,r=200){
  _ds=0;let h=await dipeek_get(x);_t=0;_tqs=0;
  let t=+dipeek_find_test_id();
  if (!h.error&&+h.simple.test_id==t){__dipeek_set_cur(h);
    return console.log(`Удалось найти попытку! (${h.simple.id})`)}
  for(let i=1;i<=r;i++){
    if(_ds)return console.log(`Поиск прерван.`);
    h=await dipeek_get(x+i);
    if (!h.error&&+h.simple.test_id==t){__dipeek_set_cur(h);
      console.log(`Удалось найти попытку! (${h.simple.id})`);return h}
    if(_ds)return console.log(`Поиск прерван.`);
    h=await dipeek_get(x-i);
    if(!h.error&&+h.simple.test_id==t){__dipeek_set_cur(h);
      console.log(`Удалось найти попытку! (${h.simple.id})`);return h}}
  console.error(`Не удалось найти попытки у ИДЫ = ${x}!`)}

let _t,_tqs,_tlq;
async function dipeek_train(x=1000000){
  console.log(`ПРЕДУПРЕЖДЕНИЕ: Это тренировочная версия скрипта! Ответы тут будут на другой тест.`);
  await new Promise(resolve => setTimeout(resolve, 5000));
  _ds=0;let h=await dipeek_get(x);__dipeek_set_cur(h);_t=1;_tqs=[];
  let us=Object.keys(h.res_array).filter(k=>k!="final_result");
  for(let ui of us){let u=h.res_array[ui];
    let ts=Object.keys(u).filter(k=>k!="didact");
    for(let ti of ts){let t=u[ti];
      _tqs.push(...Object.keys(t).filter(k=>k!="param"&&k!="result").map(k=>+k))}}
  return console.log(`Удалось найти попытку! (${h.simple.id})`)}

const _dtr=/test\/index\/(\d+)/;
function dipeek_find_test_id(){try{if(CURRENT_TEST_ID>0)return CURRENT_TEST_ID;}catch{}
  let m=_dtr.exec(document.URL);if(!m)throw new Error("Не удалось извлечь айди теста!");return+m[1]}
const _dqr=/test\/index\/\d+\/(\d+)/;
function dipeek_find_question_id(){try{if(QUESTION_ID>0)return QUESTION_ID;}catch{}
  let m=_dqr.exec(document.URL);if(!m)throw new Error("Не удалось извлечь айди вопроса!");return+m[1]}

function dipeek_get_question(i){
  let a=__dipeek_cur();i=i||dipeek_find_question_id();
  let us=Object.keys(a.res_array).filter(k=>k!="final_result");
  for(let ui of us){let u=a.res_array[ui];
    let ts=Object.keys(u).filter(k=>k!="didact");
    for(let ti of ts){let t=u[ti];
      let qs=Object.keys(t).filter(k=>k!="param"&&k!="result");
      for(let qi of qs)if(+qi==i)return t[qi]}}
  return null}
function dipeek_show_question(q){
  if(_t&&!q){if(_tlq){let ind=_tqs.indexOf(_tlq)+1;
      if(ind==_tqs.length)ind=0;_tlq=q=_tqs[ind]}
    else _tlq=q=_tqs[0];}
  if(typeof q=="number"||!q)q=dipeek_get_question(q);
  console.log(q)}

clear();
