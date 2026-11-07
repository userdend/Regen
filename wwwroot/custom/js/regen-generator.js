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
  const googleFormUrlInput = document.getElementById('googleFormUrl');

  // Modern Vertical Wizard
  // --------------------------------------------------------------------
  const wizardModernVertical = document.querySelector('.wizard-modern-vertical'),
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
    if (wizardModernVerticalBtnNextList) {
      wizardModernVerticalBtnNextList.forEach(wizardModernVerticalBtnNext => {
        wizardModernVerticalBtnNext.addEventListener('click', event => {
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
      wizardModernVerticalBtnSubmit.addEventListener('click', event => {
        Swal.fire({
          icon: 'success',
          title: 'Form Retrieved Successfully!',
          text: 'Your Google Form has been retrieved and is ready for processing.',
          confirmButtonText: 'Continue',
          confirmButtonColor: '#ff9800'
        });
      });
    }
  }
})();
