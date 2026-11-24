let cardColor,
  headingColor,
  labelColor,
  fontFamily,
  borderColor,
  legendColor,
  heatMap1,
  heatMap2,
  heatMap3,
  heatMap4,
  currentTheme,
  chartBgColor;

if (isDarkStyle) {
  heatMap1 = '#333457';
  heatMap2 = '#3c3e75';
  heatMap3 = '#484b9b';
  heatMap4 = '#696cff';
  currentTheme = 'dark';
} else {
  heatMap1 = '#ededff';
  heatMap2 = '#d5d6ff';
  heatMap3 = '#b7b9ff';
  heatMap4 = '#696cff';
  currentTheme = 'light';
}
cardColor = config.colors.cardColor;
headingColor = config.colors.headingColor;
labelColor = config.colors.textMuted;
borderColor = config.colors.borderColor;
legendColor = config.colors.bodyColor;
fontFamily = config.fontFamily;

// Chart Colors
const chartColors = {
  donut: {
    series1: config.colors.primary,
    series2: '#9055fdb3',
    series3: '#9055fd80'
  },
  donut2: {
    series1: '#49AC00',
    series2: '#4DB600',
    series3: config.colors.success,
    series4: '#78D533',
    series5: '#9ADF66',
    series6: '#BBEA99'
  },
  line: {
    series1: config.colors.warning,
    series2: config.colors.primary,
    series3: '#7367f029'
  }
};

if (isDarkStyle) {
  chartBgColor = '#474360';
  currentTheme = 'dark';
} else {
  chartBgColor = '#F0F2F8';
  currentTheme = 'light';
}

const httpStatusData = window.appUrls.httpStatusChart ?? [];

const series = httpStatusData.map(item => ({
  name: item.StatusCode,
  data: item.Data
}));

const getStatusColor = statusCode => {
  const code = Number(statusCode);

  if (code >= 100 && code < 200) return '#00cfe8';
  if (code >= 200 && code < 300) return '#28c76f';
  if (code >= 300 && code < 400) return '#00cfe8';
  if (code >= 400 && code < 500) return '#ff9f43';
  if (code >= 500 && code < 600) return '#ea5455';

  return '#82868b';
};

const statusColor = series.map(item => getStatusColor(item.name));

// Total Profit Chart
// --------------------------------------------------------------------
const totalProfitChartEl = document.querySelector('#totalProfitChart'),
  totalProfitChartConfig = {
    chart: {
      type: 'bar',
      height: 260,
      parentHeightOffset: 0,
      stacked: true,
      toolbar: { show: false }
    },
    series: series,
    plotOptions: {
      bar: {
        horizontal: false,
        columnWidth: '35%',
        borderRadius: 8,
        borderRadiusApplication: 'around',
        startingShape: 'rounded',
        endingShape: 'rounded'
      }
    },
    dataLabels: { enabled: false },
    stroke: {
      curve: 'smooth',
      width: 6,
      lineCap: 'round',
      colors: [cardColor]
    },
    legend: { show: false },
    colors: statusColor,
    grid: {
      xaxis: { lines: { show: false } },
      strokeDashArray: 8,
      borderColor,
      padding: {
        top: -10,
        left: 15,
        right: -15,
        bottom: -10
      }
    },
    xaxis: {
      axisTicks: { show: false },
      crosshairs: { opacity: 0 },
      axisBorder: { show: false },
      categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
      tickPlacement: 'on',
      labels: {
        style: {
          fontSize: '13px',
          fontFamily: fontFamily,
          colors: labelColor
        }
      }
    },
    yaxis: {
      labels: {
        style: {
          fontSize: '13px',
          fontFamily: fontFamily,
          colors: labelColor
        }
      }
    },
    states: {
      hover: { filter: { type: 'none' } },
      active: { filter: { type: 'none' } }
    },
    responsive: [
      {
        breakpoint: 1441,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '50%'
            }
          }
        }
      },
      {
        breakpoint: 1200,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '35%'
            }
          }
        }
      },
      {
        breakpoint: 1025,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '45%'
            }
          }
        }
      },
      {
        breakpoint: 767,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '35%'
            }
          }
        }
      },
      {
        breakpoint: 555,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '45%'
            }
          }
        }
      },
      {
        breakpoint: 450,
        options: {
          chart: {
            height: 200,
            offsetX: -10
          },
          plotOptions: {
            bar: {
              columnWidth: '55%'
            }
          },
          xaxis: {
            labels: {
              rotate: 315,
              rotateAlways: true
            }
          }
        }
      },
      {
        breakpoint: 360,
        options: {
          plotOptions: {
            bar: {
              columnWidth: '75%'
            }
          }
        }
      }
    ]
  };
if (typeof totalProfitChartEl !== undefined && totalProfitChartEl !== null) {
  const totalProfitChart = new ApexCharts(totalProfitChartEl, totalProfitChartConfig);
  totalProfitChart.render();
}
