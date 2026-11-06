// Core libraries
import JSZip from 'jszip';
import pdfMake from 'pdfmake/build/pdfmake';
import robotoFontContainer from 'pdfmake/build/fonts/Roboto';

// DataTables libraries (with related plugins)
import DataTable from 'datatables.net-bs5';
import 'datatables.net-fixedcolumns-bs5';
import 'datatables.net-fixedheader-bs5';
import 'datatables.net-select-bs5';
import 'datatables.net-buttons';
import 'datatables.net-buttons-bs5';
import 'datatables.net-buttons/js/buttons.html5';
import 'datatables.net-buttons/js/buttons.print';
import 'datatables.net-responsive';
import 'datatables.net-responsive-bs5';
import 'datatables.net-rowgroup-bs5';
pdfMake.addFontContainer(robotoFontContainer);

window.addEventListener(
  'keydown',
  function (e) {
    if (
      (e.key === 'Escape' || e.key === 'ArrowUp' || e.key === 'ArrowDown') &&
      e.target &&
      e.target.closest &&
      e.target.closest('.dt-button-collection')
    ) {
      e.stopPropagation();
    }
  },
  true
);

// Attach libraries to the window object (if needed globally)
try {
  window.pdfMake = pdfMake;
  window.pdfFonts = robotoFontContainer;
  window.JSZip = JSZip;
} catch (e) {}

// Export the libraries/modules
export { DataTable, JSZip, pdfMake, robotoFontContainer as pdfFonts };
