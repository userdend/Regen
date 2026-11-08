// Map a question to its corresponding function.
const questionMap = {
  0: paragraph,
  1: paragraph,
  2: multipleChoice,
  3: multipleChoice /* Drop Down. */,
  4: multipleChoice /* Checkboxes. */,
  5: multipleChoice /* Linear Scale. */,
  7: multipleChoiceGrid,
  9: date,
  10: time,
  18: multipleChoice /* Rating. */
};

function cloneQuestionChoice(question, type, required) {
  // If the question is grid-type, directly pass the question object, else, extract first then pass.
  return type == 7
    ? questionMap[type]({ question, type, required })
    : questionMap[type]({
        entryId: question[0][0],
        choices: question[0][1],
        type,
        required
      });
}

function date(questionDetail) {
  const dateInfoAlert = document.createElement('div');
  dateInfoAlert.className = 'alert alert-info';
  dateInfoAlert.role = 'alert';
  dateInfoAlert.textContent = 'A random date will be selected between the start and end dates.';

  const dateStartWrapper = document.createElement('div');
  dateStartWrapper.className = 'form-floating form-floating-outline mt-5';

  const dateEndWrapper = document.createElement('div');
  dateEndWrapper.className = 'form-floating form-floating-outline mt-5';

  // Create start date.
  const dateStart = document.createElement('input');
  const dateStartLabel = document.createElement('label');
  dateStartLabel.htmlFor = `${questionDetail.entryId}-start`;
  dateStartLabel.textContent = `Start`;

  dateStart.id = `${questionDetail.entryId}-start`;
  dateStart.type = 'date';
  dateStart.value = '';
  dateStart.className = 'form-control';

  // Create end date.
  const dateEnd = document.createElement('input');
  const dateEndLabel = document.createElement('label');
  dateEndLabel.htmlFor = `${questionDetail.entryId}-end`;
  dateEndLabel.textContent = `End`;

  dateEnd.id = `${questionDetail.entryId}-end`;
  dateEnd.type = 'date';
  dateEnd.value = '';
  dateEnd.className = 'form-control';

  // Create container.
  const container = document.createElement('div');

  dateStartWrapper.appendChild(dateStart);
  dateStartWrapper.appendChild(dateStartLabel);

  dateEndWrapper.appendChild(dateEnd);
  dateEndWrapper.appendChild(dateEndLabel);

  container.appendChild(dateInfoAlert);
  container.appendChild(dateStartWrapper);
  container.appendChild(dateEndWrapper);

  // Map the id.
  entryMapping.set(questionDetail.entryId, [questionDetail.type, questionDetail.required]);

  return container;
}

function multipleChoiceGrid(item) {
  // Create container.
  const container = document.createElement('div');

  // Item has multiple question, call multipleChoice function to clone the question.
  item.question.forEach(function (prop) {
    // Create a parent element.
    const details = document.createElement('details');
    const summary = document.createElement('summary');
    const div = document.createElement('div');

    // Extract the prop.
    const choiceEntryId = prop[0];
    const choiceTitle = prop[3][0];
    const choiceList = prop[1];

    // Extract question.
    summary.textContent = choiceTitle;

    // Call the multipleChoice function and pass the prop.
    div.appendChild(
      multipleChoice({
        entryId: choiceEntryId,
        choices: choiceList,
        type: item.type,
        required: item.required
      })
    );

    // Place all inside the parent element.
    details.appendChild(summary);
    details.appendChild(div);

    container.appendChild(details);
  });

  return container;
}

function multipleChoice(questionDetail) {
  // Remove "Other" from the choices.
  const getLastOption = questionDetail.choices.slice(-1)[0][0];

  if (getLastOption == '') {
    questionDetail.choices.pop();
  }

  // Create table for clean layout.
  const table = document.createElement('table');
  table.className = 'table table-borderless align-middle mb-0';

  // Obtain the number of choice and create an array.
  const display = new Array(questionDetail.choices.length).fill(25);

  // For each choice..
  questionDetail.choices.forEach(function (choice, index) {
    // Create an input element type range.
    const range = document.createElement('input');
    const spanDisplay = document.createElement('span');

    range.id = `${questionDetail.entryId}-${index}`;
    range.type = 'range';
    range.min = '0';
    range.value = '25';
    range.max = '50';

    // Create a display to indicate the probability of being chosen.
    spanDisplay.className = `${questionDetail.entryId}-display`;
    spanDisplay.textContent = `${(100 / questionDetail.choices.length).toFixed(1)}%`;

    // Add event listener to update the display when input being adjusted.
    range.addEventListener('input', function () {
      display[index] = parseInt(range.value, 10);

      let sum = display.reduce(function (acc, cur) {
        return acc + cur;
      }, 0);

      Array.from(document.getElementsByClassName(`${spanDisplay.className}`)).forEach(function (node, i) {
        let dist = ((display[i] / sum) * 100).toFixed(1);
        node.textContent = `${isNaN(dist) ? '0.0' : dist}%`;
      });
    });

    // Create column and row.
    const tr = document.createElement('tr');
    const tdQuestion = document.createElement('td');
    const tdInput = document.createElement('td');
    const tdDisplay = document.createElement('td');

    tdQuestion.className = 'text-muted fw-medium';
    range.className = 'form-range';
    tdDisplay.className = 'text-muted fw-medium';

    tdQuestion.id = `${questionDetail.entryId}-${index}-value`;

    // Place the element accordingly.
    tdQuestion.textContent = `${choice[0]}`;
    tdInput.appendChild(range);
    tdDisplay.appendChild(spanDisplay);

    tr.appendChild(tdQuestion);
    tr.appendChild(tdInput);
    tr.appendChild(tdDisplay);

    table.appendChild(tr);
  });

  // Map the question id.
  entryMapping.set(questionDetail.entryId, [questionDetail.type, questionDetail.required]);

  return table;
}

function paragraph(questionDetail) {
  // Create textarea element.
  const paragraphWrapper = document.createElement('div');
  const paragraphInfoAlert = document.createElement('div');
  const textareaWrapper = document.createElement('div');
  const textarea = document.createElement('textarea');
  const textareaLabel = document.createElement('label');

  paragraphInfoAlert.className = 'alert alert-info';
  paragraphInfoAlert.role = 'alert';
  paragraphInfoAlert.innerHTML = `
    <ol class="mb-0 ps-3">
        <li>Enter each answer on a separate line.</li>
        <li>For each generated response, an answer will be selected randomly from your list.</li>
        <li>To increase the likelihood of selecting a particular answer, enter it multiple times.</li>
    </ol>
    `;
  textareaWrapper.className = 'form-floating form-floating-outline';
  textarea.className = 'form-control h-px-100';
  textareaLabel.htmlFor = questionDetail.entryId;
  textareaLabel.textContent = 'List your answers';

  // Configure textarea properties.
  textarea.id = questionDetail.entryId;
  textarea.rows = '5';
  textarea.cols = '50';
  textarea.placeholder = 'Answer 1\nAnswer 2\nAnswer 3\n...';

  // Map the question id.
  entryMapping.set(questionDetail.entryId, [questionDetail.type, questionDetail.required]);

  paragraphWrapper.appendChild(paragraphInfoAlert);
  textareaWrapper.appendChild(textarea);
  textareaWrapper.appendChild(textareaLabel);
  paragraphWrapper.appendChild(textareaWrapper);

  return paragraphWrapper;
}

function time(questionDetail) {
  const timeInfoAlert = document.createElement('div');
  timeInfoAlert.className = 'alert alert-info';
  timeInfoAlert.role = 'alert';
  timeInfoAlert.textContent = 'A random time will be selected between the start and end times.';

  const timeStartWrapper = document.createElement('div');
  timeStartWrapper.className = 'form-floating form-floating-outline mt-5';

  const timeEndWrapper = document.createElement('div');
  timeEndWrapper.className = 'form-floating form-floating-outline mt-5';

  // Create start time.
  const timeStart = document.createElement('input');
  const timeStartLabel = document.createElement('label');
  timeStartLabel.htmlFor = `${questionDetail.entryId}-start`;
  timeStartLabel.textContent = `Start`;

  timeStart.id = `${questionDetail.entryId}-start`;
  timeStart.type = 'time';
  timeStart.value = '';
  timeStart.className = 'form-control';

  // Create end time.
  const timeEnd = document.createElement('input');
  const timeEndLabel = document.createElement('label');
  timeEndLabel.htmlFor = `${questionDetail.entryId}-end`;
  timeEndLabel.textContent = `End`;

  timeEnd.id = `${questionDetail.entryId}-end`;
  timeEnd.type = 'time';
  timeEnd.value = '';
  timeEnd.className = 'form-control';

  // Create container.
  const container = document.createElement('div');

  timeStartWrapper.appendChild(timeStart);
  timeStartWrapper.appendChild(timeStartLabel);

  timeEndWrapper.appendChild(timeEnd);
  timeEndWrapper.appendChild(timeEndLabel);

  container.appendChild(timeInfoAlert);
  container.appendChild(timeStartWrapper);
  container.appendChild(timeEndWrapper);

  // Map the id.
  entryMapping.set(questionDetail.entryId, [questionDetail.type, questionDetail.required]);

  return container;
}

// Type of question.
const questionType = {
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

// Indicator of a question if it is required.
const questionRequired = {
  0: "<span style='color:green;'>*</span>",
  1: "<span style='color:red;'>*</span>"
};

// List of question type that is not a question.
const questionForbidden = {
  6: null,
  8: null,
  11: null,
  12: null
};

function configureUi(data, googleFormDetailsDisplay) {
  const formContainerTemplate = document.getElementById('form-container-template');
  const questionCardTemplate = document.getElementById('question-card-template');

  const formContainerClone = formContainerTemplate.content.cloneNode(true);

  // Get the viewform pages.
  let viewformPages = 0;

  // Iterate through the trees.
  data[1][1].forEach(function (prop, index) {
    const title = prop[1];
    const description = prop[2];
    const type = prop[3];
    const options = prop[4];

    const questionCardClone = questionCardTemplate.content.cloneNode(true);

    const headerId = `accordion-header-${index}`;
    const contentId = `accordion-content-${index}`;

    questionCardClone.querySelector('.accordion-header').id = headerId;
    questionCardClone.querySelector('.accordion-button').setAttribute('data-bs-target', `#${contentId}`);
    questionCardClone.querySelector('.accordion-button').setAttribute('aria-controls', contentId);
    questionCardClone.querySelector('.accordion-collapse').id = contentId;
    questionCardClone.querySelector('.accordion-collapse').setAttribute('aria-labelledby', contentId);

    let required = ``;
    if (!questionForbidden.hasOwnProperty(type)) {
      required = prop[4][0][2];
    }

    if (type == 8) {
      viewformPages = viewformPages + 1;
    }

    // Clone the options of the corresponding question.
    if (!questionForbidden.hasOwnProperty(type)) {
      questionCardClone.querySelector('.accordion-body').appendChild(cloneQuestionChoice(options, type, required));
      questionCardClone.querySelector('.accordion-button').innerHTML = `${title}`;
    } else {
      questionCardClone.querySelector('.accordion-button').textContent = `${title}`;
    }

    // Only display the description if it is a section-like type.
    if (questionForbidden.hasOwnProperty(type)) {
      questionCardClone.querySelector('.accordion-button').classList.remove('collapsed');
      questionCardClone.querySelector('.accordion-collapse').classList.add('show');
      questionCardClone.querySelector('.accordion-body').innerHTML = `${description}`;
    }

    formContainerClone.querySelector('.accordion').appendChild(questionCardClone);
  });

  googleFormDetailsDisplay.appendChild(formContainerClone);

  // Store the viewform pages.
  localStorage.setItem('form-total-pages', viewformPages);
}
