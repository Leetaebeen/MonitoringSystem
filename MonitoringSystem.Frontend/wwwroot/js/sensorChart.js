// Blazor JS Interop 대상 모듈
// LiveDashboard의 SensorChart 컴포넌트가 IJSObjectReference로 import해서 사용함

const _charts = {};

/**
 * 차트 생성 또는 업데이트
 * 이미 canvasId로 생성된 차트가 있으면 데이터만 교체하고, 없으면 새로 생성한다.
 */
export function renderChart(canvasId, labels, temperatureData, vibrationData) {
    const existing = _charts[canvasId];
    if (existing) {
        existing.data.labels = labels;
        existing.data.datasets[0].data = temperatureData;
        existing.data.datasets[1].data = vibrationData;
        // 'none' = 애니메이션 없이 즉시 업데이트 (실시간 갱신에 적합)
        existing.update('none');
        return;
    }

    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    _charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: '온도 (°C)',
                    data: temperatureData,
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.08)',
                    yAxisID: 'yTemp',
                    tension: 0.3,
                    pointRadius: 2,
                    fill: true,
                },
                {
                    label: '진동 (mm/s)',
                    data: vibrationData,
                    borderColor: 'rgb(13, 110, 253)',
                    backgroundColor: 'rgba(13, 110, 253, 0.08)',
                    yAxisID: 'yVib',
                    tension: 0.3,
                    pointRadius: 2,
                    fill: true,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            // 초기 생성 시에도 애니메이션 끔 (실시간 데이터에서 깜빡임 방지)
            animation: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: { position: 'top' }
            },
            scales: {
                x: {
                    ticks: { maxTicksLimit: 8, maxRotation: 0 }
                },
                yTemp: {
                    type: 'linear',
                    position: 'left',
                    title: { display: true, text: '온도 (°C)' }
                },
                yVib: {
                    type: 'linear',
                    position: 'right',
                    title: { display: true, text: '진동 (mm/s)' },
                    grid: { drawOnChartArea: false }
                }
            }
        }
    });
}

/** 차트 인스턴스 해제 (컴포넌트 Dispose 시 호출) */
export function destroyChart(canvasId) {
    if (_charts[canvasId]) {
        _charts[canvasId].destroy();
        delete _charts[canvasId];
    }
}
