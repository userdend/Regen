const chartEl = document.querySelector('#hourlyOverviewChart');

let hourlyChart = null;

let borderColor, chartBgColor;

if (isDarkStyle) {
  chartBgColor = '#474360';
} else {
  chartBgColor = '#F0F2F8';
}

borderColor = config.colors.borderColor;

const hourlyOverviewChartConfig = {
  chart: {
    type: 'bar',
    height: 265,
    animations: {
      enabled: false
    },
    redrawOnWindowResize: true,
    redrawOnParentResize: true,
    toolbar: {
      show: false
    }
  },
  series: [
    {
      name: 'Completed',
      data: []
    }
  ],
  colors: [config.colors.primary],
  plotOptions: {
    bar: {
      borderRadius: 4,
      columnWidth: '65%'
    }
  },
  dataLabels: {
    enabled: false
  },
  legend: {
    show: false
  },
  grid: {
    strokeDashArray: 8,
    borderColor,
    padding: {
      bottom: 10
    }
  },
  xaxis: {
    type: 'datetime',
    labels: {
      datetimeUTC: false,
      format: 'HH:mm',
      rotate: 0,
      hideOverlappingLabels: true,
      style: {
        fontSize: '11px'
      }
    }
  },
  yaxis: {
    min: 0,
    forceNiceScale: true,
    labels: {
      formatter: function (val) {
        return Math.round(val).toString();
      }
    }
  },
  states: {
    hover: {
      filter: {
        type: 'none'
      }
    },
    active: {
      filter: {
        type: 'none'
      }
    }
  }
};

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

    if (hourlyChart) {
      const now = Date.now();
      const sixHoursAgo = now - 6 * 60 * 60 * 1000;

      await hourlyChart.updateOptions({
        xaxis: {
          type: 'datetime',
          min: sixHoursAgo,
          max: now
        },
        series: [
          {
            name: 'Completed',
            data: data.hourlyOverview.data
          }
        ]
      });
    }

    document.getElementById('hourlyTotal').textContent = data.hourlyOverview.totalCompleted.toLocaleString();
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

async function initializeDashboard() {
  if (chartEl) {
    hourlyChart = new ApexCharts(chartEl, hourlyOverviewChartConfig);

    await hourlyChart.render();
  }

  // Start polling after chart initialization
  pollDashboardStats();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initializeDashboard);
} else {
  initializeDashboard();
}

window.addEventListener('pagehide', () => {
  pollingActive = false;
});
