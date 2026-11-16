/**
 *  Form Wizard
 */

'use strict';

$(function () {
  const select2 = $('.select2'),
    selectPicker = $('.selectpicker');

  // Bootstrap select
  if (selectPicker.length) {
    selectPicker.selectpicker();
    handleBootstrapSelectEvents();
  }

  // select2
  if (select2.length) {
    select2.each(function () {
      var $this = $(this);
      select2Focus($this);
      $this.select2({
        placeholder: 'Select value',
        dropdownParent: $this.parent()
      });
    });
  }
});

(function () {
  const googleFormUrlInput = document.getElementById('google-form-url');
  const defaultNextManualBtnTemplate = document.getElementById('default-next-manual-btn-template');
  const defaultNextAiBtnTemplate = document.getElementById('default-next-ai-btn-template');
  const defaultNextLoadConfigBtnTemplate = document.getElementById('default-next-load-config-btn-template');
  const defaultLoadingDisplayBtnTemplate = document.getElementById('default-loading-display-btn-template');
  const googleFormDetailsDisplay = document.getElementById('google-form-details-display');

  let onProcessing = false;
  let onUseAi = false;

  // Modern Vertical Wizard
  // --------------------------------------------------------------------
  const wizardModernVertical = document.querySelector('.wizard-modern-vertical'),
    wizardModernVerticalBtnNextManualList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next-manual'))
      : [],
    wizardModernVerticalBtnNextAiList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next-ai'))
      : [],
    wizardModernVerticalBtnNextLoadConfigList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next-load-config'))
      : [],
    wizardModernVerticalBtnNextList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next'))
      : [],
    wizardModernVerticalBtnPrevList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-prev'))
      : [],
    wizardModernVerticalBtnSubmit = wizardModernVertical ? wizardModernVertical.querySelector('.btn-submit') : null;

  if (typeof wizardModernVertical !== undefined && wizardModernVertical !== null) {
    const modernVerticalStepper = new Stepper(wizardModernVertical, {
      linear: false
    });

    if (wizardModernVerticalBtnNextManualList) {
      wizardModernVerticalBtnNextManualList.forEach(wizardModernVerticalBtnManualNext => {
        wizardModernVerticalBtnManualNext.addEventListener('click', async event => {
          if (onProcessing) {
            return;
          }

          // Check if the input is empty.
          if (googleFormUrlInput.value.trim() === '' || !URL.canParse(googleFormUrlInput.value)) {
            Swal.fire({
              icon: 'warning',
              title: 'Warning!',
              text: 'Please enter a valid Google Form URL.',
              confirmButtonText: 'OK',
              confirmButtonColor: '#ff9800'
            });
            return;
          }

          try {
            onProcessing = true;
            wizardModernVerticalBtnManualNext.disabled = true;
            wizardModernVerticalBtnManualNext.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

            const url = await getOriginalUrl(googleFormUrlInput.value);
            localStorage.setItem('form-original-url', url);
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const response = await fetch(window.appUrls.getFormContent, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                RequestVerificationToken: token
              },
              body: JSON.stringify({ url })
            });

            if (!response.ok) {
              throw new Error(`Network response is not ok: ${response.statusText}`);
            }

            const data = await response.json();
            googleFormDetailsDisplay.innerHTML = '';
            configureUi(data, googleFormDetailsDisplay);
            modernVerticalStepper.next();
          } catch (error) {
            console.error(error);
            Swal.fire({
              icon: 'error',
              title: 'Error!',
              text: 'Something went wrong. Please try again.',
              confirmButtonText: 'OK',
              confirmButtonColor: '#d33'
            });
          } finally {
            onProcessing = false;
            onUseAi = false;
            wizardModernVerticalBtnManualNext.disabled = false;
            wizardModernVerticalBtnManualNext.innerHTML = defaultNextManualBtnTemplate.innerHTML;
          }
        });
      });
    }

    if (wizardModernVerticalBtnNextAiList) {
      wizardModernVerticalBtnNextAiList.forEach(wizardModernVerticalBtnAiNext => {
        wizardModernVerticalBtnAiNext.addEventListener('click', async event => {
          if (onProcessing) {
            return;
          }

          // Check if the input is empty.
          if (googleFormUrlInput.value.trim() === '' || !URL.canParse(googleFormUrlInput.value)) {
            Swal.fire({
              icon: 'warning',
              title: 'Warning!',
              text: 'Please enter a valid Google Form URL.',
              confirmButtonText: 'OK',
              confirmButtonColor: '#ff9800'
            });
            return;
          }

          try {
            onProcessing = true;
            wizardModernVerticalBtnAiNext.disabled = true;
            wizardModernVerticalBtnAiNext.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

            const url = await getOriginalUrl(googleFormUrlInput.value);
            localStorage.setItem('form-original-url', url);
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const response = await fetch(window.appUrls.getFormContent, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                RequestVerificationToken: token
              },
              body: JSON.stringify({ url })
            });

            if (!response.ok) {
              throw new Error(`Network response is not ok: ${response.statusText}`);
            }

            const data = await response.json();
            googleFormDetailsDisplay.innerHTML = '';
            configureUiForAi(data, googleFormDetailsDisplay);
            modernVerticalStepper.next();
          } catch (error) {
            console.error(error);
            Swal.fire({
              icon: 'error',
              title: 'Error!',
              text: 'Something went wrong. Please try again.',
              confirmButtonText: 'OK',
              confirmButtonColor: '#d33'
            });
          } finally {
            onProcessing = false;
            onUseAi = true;
            wizardModernVerticalBtnAiNext.disabled = false;
            wizardModernVerticalBtnAiNext.innerHTML = defaultNextAiBtnTemplate.innerHTML;
          }
        });
      });
    }

    if (wizardModernVerticalBtnNextLoadConfigList) {
      wizardModernVerticalBtnNextLoadConfigList.forEach(wizardModernVerticalBtnLoadConfigNext => {
        wizardModernVerticalBtnLoadConfigNext.addEventListener('click', async event => {
          if (onProcessing) {
            return;
          }

          const configId = wizardModernVerticalBtnLoadConfigNext.dataset.configId;

          try {
            onProcessing = true;
            wizardModernVerticalBtnLoadConfigNext.disabled = true;
            wizardModernVerticalBtnLoadConfigNext.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const configResponse = await fetch(
              `${window.appUrls.getSavedConfiguration}?configId=${encodeURIComponent(configId)}`,
              {
                method: 'POST',
                headers: {
                  RequestVerificationToken: token
                }
              }
            );

            if (!configResponse.ok) {
              throw new Error(`Network response is not ok: ${configResponse.statusText}`);
            }

            const configData = await configResponse.json();
            const url = configData.url;
            localStorage.setItem('form-original-url', url);
            const config = JSON.parse(configData.config);
            const interval = configData.intervalMilliseconds;

            const formResponse = await fetch(window.appUrls.getFormContent, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                RequestVerificationToken: token
              },
              body: JSON.stringify({ url })
            });

            if (!formResponse.ok) {
              throw new Error(`Network response is not ok: ${formResponse.statusText}`);
            }

            const formData = await formResponse.json();
            googleFormDetailsDisplay.innerHTML = '';
            configureUi(formData, googleFormDetailsDisplay);

            for (const [key, value] of config) {
              document.getElementById(key).value = value;
              document.getElementById(key).dispatchEvent(new Event('input', { bubbles: true }));
            }

            document.getElementById('google-form-timestamp').value = String(interval);

            modernVerticalStepper.next();
          } catch (error) {
            console.error(error);
            Swal.fire({
              icon: 'error',
              title: 'Error!',
              text: 'Something went wrong. Please try again.',
              confirmButtonText: 'OK',
              confirmButtonColor: '#d33'
            });
          } finally {
            onProcessing = false;
            onUseAi = false;
            wizardModernVerticalBtnLoadConfigNext.disabled = false;
            wizardModernVerticalBtnLoadConfigNext.innerHTML = defaultNextLoadConfigBtnTemplate.innerHTML;
          }
        });
      });
    }

    if (wizardModernVerticalBtnNextList) {
      wizardModernVerticalBtnNextList.forEach(wizardModernVerticalBtnNext => {
        wizardModernVerticalBtnNext.addEventListener('click', async event => {
          if (onProcessing) {
            return;
          }
          modernVerticalStepper.next();
        });
      });
    }

    if (wizardModernVerticalBtnPrevList) {
      wizardModernVerticalBtnPrevList.forEach(wizardModernVerticalBtnPrev => {
        wizardModernVerticalBtnPrev.addEventListener('click', event => {
          modernVerticalStepper.previous();
        });
      });
    }

    if (wizardModernVerticalBtnSubmit) {
      wizardModernVerticalBtnSubmit.addEventListener('click', async event => {
        const submitButton = event.currentTarget;

        try {
          let data = null;

          if (onUseAi) {
            // Add key to .env first.
            const promptInput = document.getElementById('prompt-input').value;
            const googleFormRespondent = document.getElementById('google-form-respondent').value;
            const response = await fetch(window.appUrls.generateSyntheticResponse, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
              },
              body: JSON.stringify({
                questions: messagePrompt,
                prompt: promptInput,
                count: googleFormRespondent
              })
            });

            if (!response.ok) {
              throw new Error('Failed to generate responses.');
            }

            const dataFromAi = await response.json();
            data = await collectEntriesFromAi(dataFromAi);
          }

          submitButton.disabled = true;
          submitButton.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

          data ??= await collectEntries();
          const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
          const request = await fetch(window.appUrls.createFormSubmission, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              RequestVerificationToken: token
            },
            body: JSON.stringify(data)
          });

          // Check if the response is OK.
          if (!request.ok) {
            throw new Error(`Network request is not ok: ${request.statusText}`);
          }

          // Parse the response JSON.
          // const response = await request.json();

          Swal.fire({
            icon: 'success',
            title: 'Form Retrieved Successfully!',
            text: 'Your Google Form has been retrieved and is ready for processing.',
            confirmButtonText: 'Continue',
            confirmButtonColor: '#ff9800'
          });
        } catch (error) {
          console.error(error);
          Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'Something went wrong. Please try again.',
            confirmButtonText: 'OK',
            confirmButtonColor: '#d33'
          });
        } finally {
          submitButton.disabled = false;
          submitButton.innerHTML = 'Submit';
        }
      });
    }
  }
})();
