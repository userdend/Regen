function configCheckboxes(item) {
  // Result.
  let answer = ``;

  const pattern = item.key;
  const inputs = document.querySelectorAll('input');

  // Get the number of input.
  const count = Array.from(inputs).filter(function (input) {
    return input.id.startsWith(pattern);
  }).length;

  // Get all input whose value is not 0.
  const nonZeroes = [];

  for (let i = 0; i < count; i++) {
    let indexValue = document.getElementById(`${pattern}-${i}`).value;

    if (indexValue != '0') {
      nonZeroes.push(i);
    }
  }

  // If all input value is zero.
  if (nonZeroes.length == 0) {
    // If the question is not mandatory, return empty string.
    if (item.required == 0) {
      return ``;
    }

    // Otherwise, return force random answer.
    const forceRandom = Math.floor(Math.random() * count);

    const forceAnswer = document.getElementById(`${item.key}-${forceRandom}-value`).textContent;

    answer = `&entry.${item.key}=${forceAnswer}`;

    return answer;
  }

  // Generate an array that contain the element based on the value amount.
  const probabilityArray = [];

  nonZeroes.forEach(function (val) {
    let indexValue = document.getElementById(`${pattern}-${val}`).value;

    for (let i = 0; i < indexValue; i++) {
      probabilityArray.push(val);
    }
  });

  // Generate a random number between 1 and the amount of non-zeroes input.
  const randomChoice = Math.floor(Math.random() * nonZeroes.length) + 1;

  // Store the temporary answer.
  const tempAnswer = [];

  // Generate answer.
  for (let i = 0; i < randomChoice; i++) {
    let theAnswer = probabilityArray[Math.floor(Math.random() * probabilityArray.length)];

    if (!tempAnswer.includes(theAnswer)) {
      tempAnswer.push(theAnswer);

      const keyContent = document.getElementById(`${item.key}-${theAnswer}-value`).textContent;

      answer += `&entry.${item.key}=${encodeURIComponent(keyContent)}`;
    }
  }

  return answer;
}

// Function converting date to yy-mm-dd format.
function yyMmDd(date) {
  const year = date.getFullYear();

  const month = String(date.getMonth() + 1).padStart(2, '0');

  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function configDate(item) {
  let answer = ``;

  // Get the date input element.
  const dateStart = document.getElementById(`${item.key}-start`);
  const dateEnd = document.getElementById(`${item.key}-end`);

  // If user does not give any input.
  if (dateStart.value == '' || dateEnd.value == '') {
    // If the question is not required, return empty string.
    if (item.required == 1) {
      return ``;

      // Otherwise return current date.
    } else {
      answer = yyMmDd(new Date());
    }
  } else {
    // Convert all to millisecond.
    const startMillis = new Date(dateStart.value).getTime();
    const endMillis = new Date(dateEnd.value).getTime();

    // Get random millisecond.
    const randomMillis = Math.random() * (endMillis - startMillis) + startMillis;

    // Convert the millisecond to date.
    const randomDate = new Date(randomMillis);

    // Transform the format.
    answer = yyMmDd(randomDate);
  }

  return `&entry.${item.key}=${encodeURIComponent(answer)}`;
}

function configMultipleChoice(item) {
  // Result.
  let answer = ``;

  const pattern = item.key;
  const inputs = document.querySelectorAll('input');

  // Get the number of input.
  const count = Array.from(inputs).filter(function (input) {
    return input.id.startsWith(pattern);
  }).length;

  // Get all input whose value is not 0.
  const nonZeroes = [];

  for (let i = 0; i < count; i++) {
    let indexValue = document.getElementById(`${pattern}-${i}`).value;

    if (indexValue != '0') {
      nonZeroes.push(i);
    }
  }

  // If all input value is zero.
  if (nonZeroes.length == 0) {
    // If the question is not mandatory, return empty string.
    if (item.required == 0) {
      return ``;
    }

    // Otherwise, return force random answer.
    const forceRandom = Math.floor(Math.random() * count);

    const forceAnswer = document.getElementById(`${item.key}-${forceRandom}-value`).textContent;

    answer = `&entry.${item.key}=${forceAnswer}`;

    return answer;
  }

  // Generate an array that contain the element based on the value amount.
  const probabilityArray = [];

  nonZeroes.forEach(function (val) {
    let indexValue = document.getElementById(`${pattern}-${val}`).value;

    for (let i = 0; i < indexValue; i++) {
      probabilityArray.push(val);
    }
  });

  // Get random answer from the probability array.
  const theAnswer = probabilityArray[Math.floor(Math.random() * probabilityArray.length)];

  const keyContent = document.getElementById(`${item.key}-${theAnswer}-value`).textContent;

  // Build the answer.
  answer += `&entry.${item.key}=${encodeURIComponent(keyContent)}`;

  return answer;
}

function configParagraph(item) {
  let answer = ``;

  // Obtain textarea document node.
  const textarea = document.getElementById(item.key);

  // If the textarea is empty...
  if (textarea.value == '') {
    if (item.required == 0) {
      // Return empty string if it is not required.
      return ``;
    } else {
      // Return an answer if otherwise.
      answer = 'No answer.';
    }
  } else {
    // Split the answer into array.
    const listAnswer = textarea.value.split('\n');

    // Choose random number from 0 to array length.
    const randomAnswer = Math.floor(Math.random() * listAnswer.length);

    // Obtain the element based on the random number.
    answer = listAnswer[randomAnswer];
  }

  return `&entry.${item.key}=${encodeURIComponent(answer)}`;
}

// Convert time to minutes.
function timeToMin(time_in_string) {
  const [hr, min] = time_in_string.split(':').map(Number);

  return hr * 60 + min;
}

// Convert minutes to time.
function minToTime(randomMin) {
  const hr = Math.floor(randomMin / 60);
  const min = randomMin % 60;

  return `${String(hr).padStart(2, '0')}:${String(min).padStart(2, '0')}`;
}

function configTime(item) {
  let answer = ``;

  // Get the input element.
  const timeStart = document.getElementById(`${item.key}-start`);
  const timeEnd = document.getElementById(`${item.key}-end`);

  // If no input value...
  if (timeStart.value == '' || timeEnd.value == '') {
    // Return empty string if not required.
    if (item.required == 1) {
      return ``;
    }

    // Otherwise return current time.
    const forceCurrentTime = new Date();
    const forceHr = String(forceCurrentTime.getHours()).padStart(2, '0');
    const forceMin = String(forceCurrentTime.getMinutes()).padStart(2, '0');

    answer = `${forceHr}:${forceMin}`;
  } else {
    // Get the input time in minutes.
    const startMin = timeToMin(timeStart.value);
    const endMin = timeToMin(timeEnd.value);

    // Get random number between the minutes.
    const randomMin = Math.floor(Math.random() * (endMin - startMin + 1)) + startMin;

    // Transform the random minutes to time.
    answer = minToTime(randomMin);
  }

  return `&entry.${item.key}=${encodeURIComponent(answer)}`;
}

// Map the question type to corresponding functions.
const configMapping = {
  0: configParagraph,
  1: configParagraph,
  2: configMultipleChoice,
  3: configMultipleChoice /* Drop Down. */,
  4: configCheckboxes /* Checkboxes. */,
  5: configMultipleChoice /* Linear Scale. */,
  7: configMultipleChoice /* Grid. */,
  9: configDate,
  10: configTime,
  18: configMultipleChoice /* Rating. */
};

// Replace component from the link.
function prefillHead() {
  const originalUrl = localStorage.getItem('originalURL');
  return originalUrl.replace('/d/', '/u/0/d/').replace('viewform', 'formResponse?');
}

// Obtain the number of pages into a component for the link.
function prefillBody() {
  let viewformPages = localStorage.getItem('form-total-pages');

  if (viewformPages > 0) {
    let pageQuery = `&pageHistory=`;

    for (let i = 0; i <= viewformPages; i++) {
      if (i != viewformPages) {
        pageQuery += `${i},`;
      } else {
        pageQuery += `${i}`;
      }
    }

    return pageQuery;
  }
}

// Obtain all the answer for corresponding question.
function prefillFoot() {
  let config = ``;

  entryMapping.forEach(function (value, key) {
    config += `${configMapping[value[0]]({ key, required: value[1] })}`;
  });

  return config;
}

async function collectEntries() {
  const googleFormRespondent = document.getElementById('google-form-respondent');
  const googleFormTimestamp = document.getElementById('google-form-timestamp');

  const urlHeader = prefillHead();
  const urlBody = prefillBody();

  entryCollection.splice(0, entryCollection.length);

  for (let i = 0; i < googleFormRespondent.value; i++) {
    entryCollection.push(`${urlHeader}${urlBody}${prefillFoot()}`);
  }

  const payload = {
    intervalMilliseconds: parseInt(googleFormTimestamp.value, 10),
    entries: entryCollection
  };

  return payload;
}
