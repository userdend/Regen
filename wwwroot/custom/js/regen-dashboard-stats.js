async function loadDashboardStats() {
  try {
    const response = await fetch(window.appUrls.getDashboardStats);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const data = await response.json();

    document.getElementById('totalScheduled').textContent = data.totalBatches.toLocaleString();
    document.getElementById('totalEntries').textContent = `${data.totalEntries.toLocaleString()} entries`;
    document.getElementById('totalCompleted').textContent = data.completed.count.toLocaleString();
    document.getElementById('completedPercentage').textContent = `${data.completed.percentage}%`;
    document.getElementById('totalPending').textContent = data.pending.count.toLocaleString();
    document.getElementById('pendingPercentage').textContent = `${data.pending.percentage}%`;
    document.getElementById('totalRetrying').textContent = data.pending.retrying.toLocaleString();
    document.getElementById('totalFailed').textContent = data.failed.count.toLocaleString();
    document.getElementById('failedPercentage').textContent = `${data.failed.percentage}%`;
  } catch (error) {
    console.error('Dashboard stats error:', error);
  }
}

let pollingActive = true;

async function pollDashboardStats() {
  if (!pollingActive) return;

  await loadDashboardStats();

  if (pollingActive) {
    setTimeout(pollDashboardStats, 15000);
  }
}

async function loadFailedBatches() {
  const response = await fetch(window.appUrls.getFailedBatches);
  if (!response.ok) return;

  const batches = await response.json();
  const tbody = document.getElementById('failedBatchesTable');

  tbody.replaceChildren();

  if (batches.length === 0) {
    tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center">
                    No failed batches found.
                </td>
            </tr>
        `;
    return;
  }

  const template = document.getElementById('failedBatchRowTemplate');

  batches.forEach(batch => {
    const row = template.content.firstElementChild.cloneNode(true);

    row.querySelector('.batch-id').textContent = batch.id.substring(0, 8);
    row.querySelector('.batch-created').textContent = new Date(batch.createdAt).toLocaleString();
    row.querySelector('.batch-total').textContent = batch.totalEntries;
    row.querySelector('.batch-completed').textContent = batch.completedEntries;
    row.querySelector('.batch-failed').textContent = batch.failedEntries;

    row.querySelector('.retry-btn').addEventListener('click', () => {
      retryBatch(batch.id);
    });

    row.classList.add('text-nowrap');

    tbody.appendChild(row);
  });
}

async function retryBatch(batchId) {
  const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

  try {
    const response = await fetch(`${window.appUrls.retryFailedBatches}?batchId=${encodeURIComponent(batchId)}`, {
      method: 'POST',
      headers: {
        RequestVerificationToken: token
      }
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || 'Something went wrong.');
    }

    await Swal.fire({
      icon: 'success',
      title: 'Success!',
      text: data.message,
      timer: 1500,
      customClass: {
        confirmButton: 'btn btn-primary me-2 waves-effect waves-light',
        cancelButton: 'btn btn-outline-secondary waves-effect'
      },
      buttonsStyling: false
    });

    await loadFailedBatches();
    await loadDashboardStats();
  } catch (error) {
    Swal.fire({
      icon: 'error',
      title: 'Error!',
      text: error.message,
      confirmButtonText: 'OK'
    });
  }
}

async function initializeDashboard() {
  pollDashboardStats();
  loadFailedBatches();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initializeDashboard);
} else {
  initializeDashboard();
}

window.addEventListener('pagehide', () => {
  pollingActive = false;
});
