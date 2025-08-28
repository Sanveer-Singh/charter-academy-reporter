// Charter Reporter App - Dashboard Module

const Dashboard = (function() {
    'use strict';
    
    // Module configuration
    const config = {
        endpoints: {
            getData: '/api/dashboard/data',
            exportData: '/api/dashboard/export',
            updateFilters: '/api/dashboard/filter'
        },
        refreshInterval: 300000, // 5 minutes
        chartColors: {
            primary: 'rgb(30, 58, 138)',
            success: 'rgb(16, 185, 129)',
            warning: 'rgb(245, 158, 11)',
            danger: 'rgb(239, 68, 68)',
            info: 'rgb(6, 182, 212)',
            secondary: 'rgb(156, 163, 175)'
        }
    };
    
    // Chart instances
    let enrollmentChart = null;
    let distributionChart = null;
    let currentFilters = {};
    let currentRole = '';
    
    // Initialize dashboard
    const init = function(options = {}) {
        currentRole = options.role || 'Charter-Admin';
        
        // Initialize filters
        initializeFilters();
        
        // Initialize charts
        if (options.chartData) {
            initializeCharts(options.chartData);
        }
        
        // Initialize event handlers
        initializeEventHandlers();
        
        // Start auto-refresh
        startAutoRefresh();
        
        // Initialize tooltips
        $('[data-toggle="tooltip"]').tooltip();
    };
    
    // Initialize filters
    const initializeFilters = function() {
        const filterForm = document.getElementById('dashboardFilters');
        if (filterForm) {
            filterForm.addEventListener('submit', function(e) {
                e.preventDefault();
                applyFilters();
            });
        }
        
        // Handle date range change
        const dateRangeSelect = document.getElementById('dateRange');
        if (dateRangeSelect) {
            dateRangeSelect.addEventListener('change', function() {
                if (this.value === 'custom') {
                    showCustomDatePicker();
                }
            });
        }
    };
    
    // Initialize event handlers
    const initializeEventHandlers = function() {
        // Chart period buttons
        document.querySelectorAll('.chart-container__action').forEach(button => {
            button.addEventListener('click', function() {
                const period = this.dataset.period;
                updateChartPeriod(period, this);
            });
        });
        
        // Funnel stage hover effect
        document.querySelectorAll('.funnel-stage').forEach(stage => {
            stage.addEventListener('mouseenter', function() {
                this.style.transform = 'scale(1.02)';
            });
            stage.addEventListener('mouseleave', function() {
                this.style.transform = 'scale(1)';
            });
        });
    };
    
    // Initialize charts
    const initializeCharts = function(chartData) {
        // Initialize enrollment trend chart
        const enrollmentCtx = document.getElementById('enrollmentChart');
        if (enrollmentCtx) {
            enrollmentChart = new Chart(enrollmentCtx, {
                type: 'line',
                data: chartData.enrollment,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            mode: 'index',
                            intersect: false,
                            callbacks: {
                                label: function(context) {
                                    let label = context.dataset.label || '';
                                    if (label) {
                                        label += ': ';
                                    }
                                    label += CharterApp.formatNumber(context.parsed.y);
                                    return label;
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            grid: {
                                display: false
                            }
                        },
                        y: {
                            beginAtZero: true,
                            ticks: {
                                callback: function(value) {
                                    return CharterApp.formatNumber(value);
                                }
                            }
                        }
                    },
                    interaction: {
                        mode: 'nearest',
                        axis: 'x',
                        intersect: false
                    }
                }
            });
            
            // Create custom legend
            createCustomLegend(enrollmentChart, 'enrollmentLegend');
        }
        
        // Initialize distribution chart
        const distributionCtx = document.getElementById('distributionChart');
        if (distributionCtx) {
            distributionChart = new Chart(distributionCtx, {
                type: 'doughnut',
                data: chartData.distribution,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.parsed;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = ((value / total) * 100).toFixed(1);
                                    return `${label}: ${percentage}%`;
                                }
                            }
                        }
                    }
                }
            });
            
            // Create custom legend
            createCustomLegend(distributionChart, 'distributionLegend');
        }
    };
    
    // Create custom legend
    const createCustomLegend = function(chart, legendId) {
        const legendContainer = document.getElementById(legendId);
        if (!legendContainer) return;
        
        const datasets = chart.data.datasets;
        const labels = chart.data.labels;
        
        legendContainer.innerHTML = '';
        
        if (chart.config.type === 'doughnut' || chart.config.type === 'pie') {
            // For pie/doughnut charts
            labels.forEach((label, index) => {
                const color = datasets[0].backgroundColor[index];
                const legendItem = createLegendItem(label, color);
                legendContainer.appendChild(legendItem);
            });
        } else {
            // For other charts
            datasets.forEach(dataset => {
                const legendItem = createLegendItem(dataset.label, dataset.borderColor);
                legendContainer.appendChild(legendItem);
            });
        }
    };
    
    // Create legend item
    const createLegendItem = function(label, color) {
        const item = document.createElement('div');
        item.className = 'chart-legend__item';
        
        const colorBox = document.createElement('span');
        colorBox.className = 'chart-legend__color';
        colorBox.style.backgroundColor = color;
        
        const text = document.createElement('span');
        text.textContent = label;
        
        item.appendChild(colorBox);
        item.appendChild(text);
        
        return item;
    };
    
    // Apply filters
    const applyFilters = async function() {
        const formData = new FormData(document.getElementById('dashboardFilters'));
        const filters = Object.fromEntries(formData.entries());
        
        // Show loading state
        CharterApp.Loading.show('Updating dashboard...');
        
        try {
            const response = await CharterApp.Ajax.post(config.endpoints.updateFilters, filters);
            
            if (response.success) {
                // Update metrics
                updateMetrics(response.metrics);
                
                // Update charts
                updateCharts(response.chartData);
                
                // Update table
                updateRecentActivity(response.recentActivity);
                
                // Store current filters
                currentFilters = filters;
                
                CharterApp.Toast.success('Dashboard updated');
            } else {
                CharterApp.Toast.error('Failed to update dashboard');
            }
        } catch (error) {
            console.error('Filter error:', error);
            CharterApp.Toast.error('An error occurred while updating the dashboard');
        } finally {
            CharterApp.Loading.hide();
        }
    };
    
    // Reset filters
    const resetFilters = function() {
        const form = document.getElementById('dashboardFilters');
        if (form) {
            form.reset();
            // Set default values
            document.getElementById('dateRange').value = '30';
            applyFilters();
        }
    };
    
    // Update metrics
    const updateMetrics = function(metrics) {
        // Update enrollment metric
        updateMetricCard('enrollments', metrics.enrollments);
        
        // Update sales metric (Charter Admin only)
        if (currentRole === 'Charter-Admin') {
            updateMetricCard('sales', metrics.sales);
        }
        
        // Update completions metric
        updateMetricCard('completions', metrics.completions);
        
        // Update completion rate
        updateMetricCard('completionRate', metrics.completionRate);
    };
    
    // Update individual metric card
    const updateMetricCard = function(type, data) {
        const card = document.querySelector(`.metric-card[data-metric="${type}"]`);
        if (!card) return;
        
        // Update value
        const valueElement = card.querySelector('.metric-card__value');
        if (valueElement) {
            if (type === 'sales') {
                valueElement.textContent = CharterApp.formatCurrency(data.value, 'ZAR');
            } else if (type === 'completionRate') {
                valueElement.textContent = `${data.value}%`;
            } else {
                valueElement.textContent = CharterApp.formatNumber(data.value);
            }
        }
        
        // Update change
        const changeElement = card.querySelector('.metric-card__change');
        if (changeElement && data.change !== undefined) {
            const isPositive = data.change >= 0;
            changeElement.className = `metric-card__change metric-card__change--${isPositive ? 'positive' : 'negative'}`;
            
            const icon = changeElement.querySelector('.metric-card__change-icon');
            if (icon) {
                icon.className = `fas fa-arrow-${isPositive ? 'up' : 'down'} metric-card__change-icon`;
            }
            
            const changeText = changeElement.querySelector('span');
            if (changeText) {
                changeText.textContent = `${Math.abs(data.change)}% from last month`;
            }
        }
    };
    
    // Update charts
    const updateCharts = function(chartData) {
        // Update enrollment chart
        if (enrollmentChart && chartData.enrollment) {
            enrollmentChart.data = chartData.enrollment;
            enrollmentChart.update();
        }
        
        // Update distribution chart
        if (distributionChart && chartData.distribution) {
            distributionChart.data = chartData.distribution;
            distributionChart.update();
        }
    };
    
    // Update chart period
    const updateChartPeriod = function(period, button) {
        // Update active state
        button.parentElement.querySelectorAll('.chart-container__action').forEach(btn => {
            btn.classList.remove('chart-container__action--active');
        });
        button.classList.add('chart-container__action--active');
        
        // In a real implementation, this would fetch new data
        CharterApp.Toast.info(`Chart period changed to ${period}`);
    };
    
    // Export data
    const exportData = function() {
        $('#exportModal').modal('show');
    };
    
    // Confirm export
    const confirmExport = async function() {
        const format = document.getElementById('exportFormat').value;
        
        if (!format) {
            CharterApp.Toast.error('Please select an export format');
            return;
        }
        
        // Get selected data options
        const includeOptions = {
            metrics: document.getElementById('includeMetrics').checked,
            enrollments: document.getElementById('includeEnrollments').checked,
            sales: document.getElementById('includeSales').checked,
            completions: document.getElementById('includeCompletions').checked
        };
        
        // Hide modal
        $('#exportModal').modal('hide');
        
        // Show loading
        CharterApp.Loading.show('Generating export...');
        
        try {
            const response = await CharterApp.Ajax.post(config.endpoints.exportData, {
                format: format,
                filters: currentFilters,
                include: includeOptions
            });
            
            if (response.success) {
                // In a real implementation, this would download the file
                CharterApp.Toast.success(`Export generated successfully`);
                
                // Simulate file download
                const link = document.createElement('a');
                link.href = response.downloadUrl;
                link.download = `dashboard-report-${CharterApp.formatDate(new Date(), 'short').replace(/\s/g, '-')}.${format}`;
                link.click();
            } else {
                CharterApp.Toast.error('Failed to generate export');
            }
        } catch (error) {
            console.error('Export error:', error);
            CharterApp.Toast.error('An error occurred while generating the export');
        } finally {
            CharterApp.Loading.hide();
        }
    };
    
    // Show custom date picker
    const showCustomDatePicker = function() {
        // In a real implementation, this would show a date range picker
        CharterApp.Toast.info('Custom date range picker would appear here');
    };
    
    // Update recent activity table
    const updateRecentActivity = function(activities) {
        const tbody = document.querySelector('.data-table tbody');
        if (!tbody || !activities) return;
        
        tbody.innerHTML = '';
        
        activities.forEach(activity => {
            const row = createActivityRow(activity);
            tbody.appendChild(row);
        });
    };
    
    // Create activity row
    const createActivityRow = function(activity) {
        const row = document.createElement('tr');
        
        row.innerHTML = `
            <td>${activity.studentName}</td>
            <td>${activity.email}</td>
            <td>${activity.course}</td>
            <td><span class="badge badge-${getCategoryBadgeClass(activity.category)}">${activity.category}</span></td>
            <td>${CharterApp.formatDate(activity.enrolledDate, 'short')}</td>
            <td><span class="badge badge-${getStatusBadgeClass(activity.status)}">${activity.status}</span></td>
            <td class="hide-mobile">
                <div class="progress" style="height: 20px;">
                    <div class="progress-bar bg-${getProgressBarClass(activity.progress)}" 
                         role="progressbar" 
                         style="width: ${activity.progress}%"
                         aria-valuenow="${activity.progress}" 
                         aria-valuemin="0" 
                         aria-valuemax="100">${activity.progress}%</div>
                </div>
            </td>
        `;
        
        return row;
    };
    
    // Get category badge class
    const getCategoryBadgeClass = function(category) {
        const classes = {
            'Professional': 'info',
            'Technical': 'primary',
            'Compliance': 'warning',
            'Leadership': 'secondary'
        };
        return classes[category] || 'secondary';
    };
    
    // Get status badge class
    const getStatusBadgeClass = function(status) {
        const classes = {
            'Active': 'success',
            'Completed': 'secondary',
            'Pending': 'warning'
        };
        return classes[status] || 'secondary';
    };
    
    // Get progress bar class
    const getProgressBarClass = function(progress) {
        if (progress >= 100) return 'secondary';
        if (progress >= 75) return 'success';
        if (progress >= 50) return 'warning';
        return 'danger';
    };
    
    // Start auto-refresh
    const startAutoRefresh = function() {
        setInterval(() => {
            // In a real implementation, this would fetch fresh data
            console.log('Dashboard auto-refresh triggered');
        }, config.refreshInterval);
    };
    
    // Public API
    return {
        init: init,
        exportData: exportData,
        confirmExport: confirmExport,
        resetFilters: resetFilters,
        applyFilters: applyFilters
    };
})();

// Export for global access
window.Dashboard = Dashboard;