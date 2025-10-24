// wwwroot/js/chartManager.js - versión mejorada
class ChartManager {
    constructor() {
        this.charts = new Map();
    }

    initializeAllCharts() {
        console.log('Inicializando todos los gráficos...');
        this.initializeChart('monthlyTrendChart');
        this.initializeChart('expenseChart');
        this.initializeChart('incomeChart');
        this.initializeChart('budgetProgressChart');
    }

    initializeChart(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.warn(`Canvas no encontrado: ${canvasId}`);
            return;
        }

        const chartDataJson = canvas.getAttribute('data-chart-data');
        if (!chartDataJson) {
            console.warn(`No hay datos para el gráfico: ${canvasId}`);
            return;
        }

        try {
            const chartData = JSON.parse(chartDataJson);
            console.log(`Datos del gráfico ${canvasId}:`, chartData);
            
            const ctx = canvas.getContext('2d');
            
            const chart = new Chart(ctx, {
                type: chartData.type,
                data: {
                    labels: chartData.labels,
                    datasets: chartData.datasets.map(dataset => this.mapDataset(dataset))
                },
                options: this.getChartOptions(chartData, canvasId)
            });

            this.charts.set(canvasId, chart);
            console.log(`Gráfico ${canvasId} inicializado correctamente`);
        } catch (error) {
            console.error(`Error al inicializar gráfico ${canvasId}:`, error);
        }
    }

    mapDataset(dataset) {
        return {
            label: dataset.label,
            data: dataset.data,
            backgroundColor: dataset.backgroundColor,
            borderColor: dataset.borderColor,
            borderWidth: dataset.borderWidth,
            fill: dataset.fill,
            tension: dataset.tension || 0.4,
            // Propiedades de puntos
            pointBackgroundColor: dataset.pointBackgroundColor,
            pointBorderColor: dataset.pointBorderColor,
            pointBorderWidth: dataset.pointBorderWidth,
            pointRadius: dataset.pointRadius,
            pointHoverRadius: dataset.pointHoverRadius,
            pointStyle: dataset.pointStyle,
            borderDash: dataset.borderDash
        };
    }

    getChartOptions(chartData, canvasId) {
        const baseOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: chartData.options?.plugins?.legend?.display ?? true,
                    position: chartData.options?.plugins?.legend?.position ?? 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: {
                            size: 12,
                            family: "'Inter', 'Segoe UI', sans-serif"
                        }
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    backgroundColor: 'rgba(15, 23, 42, 0.9)',
                    titleColor: '#f1f5f9',
                    bodyColor: '#f1f5f9',
                    borderColor: 'rgba(255, 255, 255, 0.1)',
                    borderWidth: 1,
                    cornerRadius: 8,
                    usePointStyle: true,
                    padding: 12,
                    callbacks: {
                        label: (context) => {
                            let label = context.dataset.label || '';
                            if (label) label += ': ';
                            if (context.parsed.y !== null) {
                                label += new Intl.NumberFormat('es-ES', {
                                    style: 'currency',
                                    currency: 'USD'
                                }).format(context.parsed.y);
                            }
                            return label;
                        }
                    }
                }
            },
            interaction: {
                mode: 'index',
                intersect: false
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            },
            elements: {
                line: {
                    tension: 0.4
                },
                point: {
                    hoverRadius: 8,
                    hoverBorderWidth: 3,
                    radius: 4
                }
            }
        };

        // Configuración específica por tipo de gráfico
        switch (chartData.type) {
            case 'line':
                baseOptions.scales = {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.08)',
                            drawBorder: false
                        },
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            },
                            font: {
                                size: 11,
                                family: "'Inter', 'Segoe UI', sans-serif"
                            },
                            color: '#64748b',
                            padding: 8
                        }
                    },
                    x: {
                        grid: {
                            color: 'rgba(0, 0, 0, 0.04)',
                            drawBorder: false
                        },
                        ticks: {
                            font: {
                                size: 11,
                                family: "'Inter', 'Segoe UI', sans-serif"
                            },
                            color: '#64748b',
                            padding: 8
                        }
                    }
                };
                break;

            case 'doughnut':
            case 'pie':
                baseOptions.plugins.tooltip.callbacks.label = function(context) {
                    const label = context.label || '';
                    const value = context.parsed;
                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                    const percentage = Math.round((value / total) * 100);
                    return `${label}: $${value.toFixed(2)} (${percentage}%)`;
                };
                baseOptions.cutout = chartData.type === 'doughnut' ? '60%' : '0%';
                break;

            case 'bar':
                baseOptions.scales = {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.08)',
                            drawBorder: false
                        },
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            },
                            font: {
                                size: 11,
                                family: "'Inter', 'Segoe UI', sans-serif"
                            },
                            color: '#64748b'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            font: {
                                size: 11,
                                family: "'Inter', 'Segoe UI', sans-serif"
                            },
                            color: '#64748b'
                        }
                    }
                };
                break;
        }

        return baseOptions;
    }

    destroyChart(canvasId) {
        if (this.charts.has(canvasId)) {
            this.charts.get(canvasId).destroy();
            this.charts.delete(canvasId);
        }
    }

    refreshAllCharts() {
        this.charts.forEach((chart, canvasId) => {
            this.destroyChart(canvasId);
        });
        this.initializeAllCharts();
    }
}

// Inicializar al cargar la página
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM cargado, inicializando ChartManager...');
    window.chartManager = new ChartManager();
    window.chartManager.initializeAllCharts();
});