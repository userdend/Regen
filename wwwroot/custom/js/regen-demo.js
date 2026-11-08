// Type of question.
const question_type = {
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
const question_required = {
  0: "<span style='color:green;'>*</span>",
  1: "<span style='color:red;'>*</span>"
};

// List of question type that is not a question.
const question_forbidden = {
  6: null,
  8: null,
  11: null,
  12: null
};

const formContainerTemplate = document.getElementById('form-container-template');
const questionCardTemplate = document.getElementById('question-card-template');
const demoContent = document.getElementById('demo-content');
const demoInput = document.getElementById('demo-input');

function startDemo(data_samples) {
  const formContainerClone = formContainerTemplate.content.cloneNode(true);

  // Loop through the samples.
  data_samples[1][1].forEach(function (prop, index) {
    const title = prop[1];
    const type = prop[3];

    const questionCardClone = questionCardTemplate.content.cloneNode(true);

    const headerId = `accordion-header-${index}`;
    const contentId = `accordion-content-${index}`;

    questionCardClone.querySelector('.accordion-header').id = headerId;
    questionCardClone.querySelector('.accordion-button').setAttribute('data-bs-target', `#${contentId}`);
    questionCardClone.querySelector('.accordion-button').setAttribute('aria-controls', contentId);
    questionCardClone.querySelector('.accordion-collapse').id = contentId;
    questionCardClone.querySelector('.accordion-collapse').setAttribute('aria-labelledby', contentId);

    // Check the question if it's required to answer or not.
    let required = ``;

    if (!question_forbidden.hasOwnProperty(type)) {
      required = prop[4][0][2];

      questionCardClone.querySelector('.accordion-button').innerHTML = `${title}`;
    } else {
      questionCardClone.querySelector('.accordion-button').textContent = `${title}`;
    }

    questionCardClone.querySelector('.accordion-body').textContent = 'Login to access full feature!';
    formContainerClone.querySelector('.accordion').appendChild(questionCardClone);
  });

  demoContent.appendChild(formContainerClone);
}

document.getElementById('btn-try')?.addEventListener('click', async () => {
  // Check if the input is empty.
  if (demoInput.value.trim() === '' || !URL.canParse(demoInput.value)) {
    Swal.fire({
      icon: 'warning',
      title: 'Warning!',
      text: 'Please enter a valid Google Form URL.',
      confirmButtonText: 'OK',
      confirmButtonColor: '#ff9800'
    });
    return;
  }

  demoContent.classList.remove('d-none');
  demoContent.innerHTML = "<div class='spinner-border' role='status'></div>";

  try {
    const url = await getOriginalUrl(demoInput.value);

    // Send request to API.
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const response = await fetch(window.appUrls.getFormContent, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        RequestVerificationToken: token
      },
      body: JSON.stringify({ url })
    });

    // Check if the response is OK.
    if (!response.ok) {
      throw new Error(`Network response is not ok: ${response.statusText}`);
    }

    // Parse the response JSON.
    const data = await response.json();

    // Start the demo with the received data.
    demoContent.innerHTML = '';
    startDemo(data);
  } catch (error) {
    demoContent.innerHTML = '';
    demoContent.classList.add('d-none');

    Swal.fire({
      icon: 'error',
      title: 'Error!',
      text: 'Something went wrong. Please try again.',
      confirmButtonText: 'OK',
      confirmButtonColor: '#d33'
    });
  }
});
