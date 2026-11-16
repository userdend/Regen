// Map a question to its corresponding function.
const questionFn = {
  0: paragraphFn,
  1: paragraphFn,
  2: multipleFn,
  3: multipleFn /* Drop Down. */,
  4: multipleFn /* Checkboxes. */,
  5: multipleFn /* Linear Scale. */,
  7: gridFn,
  9: dateFn,
  10: timeFn,
  18: multipleFn /* Rating. */
};

// List of question type that is not a question.
const defForbiddenType = {
  6: null,
  8: null,
  11: null,
  12: null
};

// Type of question.
const questionType1 = {
  0: 'Short Answer',
  1: 'Long Answer',
  2: 'Multiple-Choice',
  3: 'Drop Down',
  4: 'Checkboxes',
  5: 'Linear Scale',
  6: 'Title & Description',
  7: 'Grid',
  8: 'Section',
  9: 'Date',
  10: 'Time',
  11: 'Image',
  12: 'Video',
  18: 'Rating'
};

function paragraphFn(data) {
  const id = data[4][0][0];
  const question = data[1];
  const type = questionType1[data[3]];
  const rule = 'Short answer only';
  return { id, question, type, rule };
}

function multipleFn(data) {
  const id = data[4][0][0];
  const question = data[1];
  const type = questionType1[data[3]];
  const options = data[4][0][1].map(first => first[0]);
  return { id, question, type, options };
}

function gridFn(data, index) {
  const mainQuestion = data[1];
  data[4].forEach(function (prop, _index) {
    const subQuestion = prop[3][0];
    const question = `${mainQuestion}: ${subQuestion}`;
    const type = questionType1[2];
    const id = prop[0];
    const options = prop[1].map(first => first[0]);
    messagePrompt[`question_${index}_${_index}`] = {
      id,
      question,
      type,
      options
    };
  });
}

function dateFn(data) {
  const id = data[4][0][0];
  const question = data[1];
  const type = questionType1[data[3]];
  const format = 'YYYY-MM-DD';
  return { id, question, type, format };
}

function timeFn(data) {
  const id = data[4][0][0];
  const question = data[1];
  const type = questionType1[data[3]];
  const format = 'HH:mm';
  return { id, question, type, format };
}

function configureUiForAi(data, googleFormDetailsDisplay) {
  let index = 1;
  data[1][1].forEach(function (prop) {
    const type = prop[3];

    if (!defForbiddenType.hasOwnProperty(type)) {
      if (type !== 7) {
        messagePrompt[`question_${index}`] = questionFn[type](prop);
      } else {
        questionFn[type](prop, index);
      }
      index++;
    }
  });

  const promptTemplate = document.getElementById('prompt-template');
  googleFormDetailsDisplay.innerHTML = promptTemplate.innerHTML;
}
