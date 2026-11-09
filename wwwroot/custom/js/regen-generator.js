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
  const defaultLoadingDisplayBtnTemplate = document.getElementById('default-loading-display-btn-template');
  const googleFormDetailsDisplay = document.getElementById('google-form-details-display');

  let onProcessing = false;
  let onManual = true;

  // Modern Vertical Wizard
  // --------------------------------------------------------------------
  const wizardModernVertical = document.querySelector('.wizard-modern-vertical'),
    wizardModernVerticalBtnNextManualList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next-manual'))
      : [],
    wizardModernVerticalBtnNextAiList = wizardModernVertical
      ? [].slice.call(wizardModernVertical.querySelectorAll('.btn-next-ai'))
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
            onManual = true;
            wizardModernVerticalBtnManualNext.disabled = true;
            wizardModernVerticalBtnManualNext.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

            const url = await getOriginalUrl(googleFormUrlInput.value);
            localStorage.setItem('originalURL', url);
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
            onManual = true;
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
          modernVerticalStepper.next();
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
          submitButton.disabled = true;
          submitButton.innerHTML = defaultLoadingDisplayBtnTemplate.innerHTML;

          const data = await collectEntries();
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
          const response = await request.json();

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
