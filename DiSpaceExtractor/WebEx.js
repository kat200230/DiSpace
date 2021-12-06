const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function getAttempt(id) {
  return new Promise(resolve => {
    $.ajax({
      type: 'POST',
      url: '/ditest/index/result',
      data: {
        action: 'get_results_by_test',
        score_id: id,
        test_id: 40000
      },
      dataType: 'json',
      success: (data, status, xhr) => resolve(data),
      error: (xhr, status, err) => {
        console.log(`ExtractionError: ${err} (${status})`);
        resolve({ error: `ExtractionError: ${err} (${status})` });
      },
    });
  });
}

async function getDate(id) {
	let i = 0;
	let max = 20;
	while (i < max) {
		let res = await getAttempt(id + i);
		if (res && !res.error) return res.simple.test_start_time;
		i++;
	}
}

async function getAttempts(from, to, chunkSize = 100, delaySeconds = 2) {
  let all = [];
  let curIndex = from;
  while (curIndex <= to)
  {
    let thisChunkSize = Math.min(chunkSize, to - curIndex + 1);
    let chunkPromises = [];
    for (let i = 0; i < thisChunkSize; i++)
    {
      let myPromise = getAttempt(curIndex + i);
      chunkPromises.push(myPromise);
    }
    let chunkComplete = await Promise.all(chunkPromises);
    all.push(...chunkComplete);
    console.log(`Wrote ${chunkComplete.filter(c => !c.error)}/${thisChunkSize} results (${curIndex}-${curIndex + thisChunkSize - 1})`);
    curIndex += thisChunkSize;
    if (curIndex <= to) await sleep(delaySeconds * 1000);
  }
  return all;
}
async function getAttemptsHandler(from, to, chunkSize = 100, delaySeconds = 2, chunkHandler) {
  let curIndex = from;
  while (curIndex <= to)
  {
    let thisChunkSize = Math.min(chunkSize, to - curIndex + 1);
    let chunkPromises = [];
    for (let i = 0; i < thisChunkSize; i++)
    {
      let myPromise = getAttempt(curIndex + i);
      chunkPromises.push(myPromise);
    }
    let chunkComplete = await Promise.all(chunkPromises);
    await chunkHandler(curIndex, curIndex + thisChunkSize - 1, chunkComplete);
    curIndex += thisChunkSize;
    if (curIndex <= to) await sleep(delaySeconds * 1000);
  }
}

function addButton(text, onclick, clickData) {
  let parentDiv = document.querySelector("#outer > div.page-container > div.tab-container > nav > div > ul");
  let button = document.createElement("button", { type: "button" });
  let label = document.createTextNode(text);
  button.appendChild(label);

  button = parentDiv.appendChild(button);
  button.onclick = async () => await onclick(button);
}

async function writeFile(name, json) {
  const file = await window.showSaveFilePicker({
    suggestedName: name,
  });
  const writable = await file.createWritable();
  if (typeof json !== "string") json = JSON.stringify(json);
  await writable.write(json);
  await writable.close();
  console.log(`Wrote data to ${file.name} file.`);
}

async function sink(from, to, eachN = 10, chunkSize = 100, delaySeconds = 5) {
  let cxt = { arr: [], start: null };
  let i = 0;
  await getAttemptsHandler(
    from, to, chunkSize, delaySeconds,
    async (start, end, chunk) => {

      if (!cxt.start) cxt.start = start;
      cxt.arr.push(...chunk);

      if (++i == eachN) {
        i = 0;
        let mySlice = cxt.arr;
        let myStart = cxt.start;
        cxt.arr = [];
        cxt.start = null;

        addButton(`${myStart}-${end}`, async (button) => {
          await writeFile(`${myStart}-${end}.json`, mySlice);
          button.remove();
        });
      }

    });
}

clear();
