(function () {
    var startBtn = document.getElementById('startOptBtn');
    var pollTimer = null;

    // The most recently rendered result payload, so option tabs can re-render
    // without re-fetching.
    var currentResult = null;
    var selectedOptionKey = null;

    // Mirrors ParameterSweep.Expand() in OptimizationProfile.cs.
    function expandSweep(min, max, step) {
        if (min == null || max == null || step == null) return null;
        if (max <= min || step <= 0) return null;

        var values = [];
        for (var v = min; v <= max + 1e-9; v += step) {
            values.push(Math.round(v * 1e6) / 1e6);
        }
        if (values.length === 0 || values[values.length - 1] < max) values.push(max);

        return Array.from(new Set(values)).sort(function (a, b) { return a - b; });
    }

    function getVal(id) {
        var el = document.getElementById(id);
        if (!el) return null;
        var v = el.value.trim();
        return v === '' ? null : parseFloat(v);
    }

    function buildProfileDto() {
        var indicatorsMap = {};
        document.querySelectorAll('.opt-input').forEach(function (input) {
            var id = input.getAttribute('data-indicator-id');
            var field = input.getAttribute('data-field');
            var v = input.value.trim();
            if (!indicatorsMap[id]) indicatorsMap[id] = { indicatorId: parseInt(id, 10) };
            indicatorsMap[id][field] = v === '' ? null : parseFloat(v);
        });

        return {
            indicators: Object.values(indicatorsMap),
            slMin: getVal('slMin'), slMax: getVal('slMax'), slStep: getVal('slStep'),
            tpMin: getVal('tpMin'), tpMax: getVal('tpMax'), tpStep: getVal('tpStep'),
            tsMin: getVal('tsMin'), tsMax: getVal('tsMax'), tsStep: getVal('tsStep'),
            psMin: getVal('psMin'), psMax: getVal('psMax'), psStep: getVal('psStep')
        };
    }

    function estimateCombinations(dto) {
        var total = 1;
        var axisCount = 0;

        dto.indicators.forEach(function (ind) {
            var p = expandSweep(ind.periodMin, ind.periodMax, ind.periodStep);
            if (p) { total *= p.length; axisCount++; }
            var t = expandSweep(ind.thresholdMin, ind.thresholdMax, ind.thresholdStep);
            if (t) { total *= t.length; axisCount++; }
        });

        ['sl', 'tp', 'ts', 'ps'].forEach(function (prefix) {
            var s = expandSweep(dto[prefix + 'Min'], dto[prefix + 'Max'], dto[prefix + 'Step']);
            if (s) { total *= s.length; axisCount++; }
        });

        return { total: axisCount === 0 ? 1 : total, axisCount: axisCount };
    }

    function updateEstimate() {
        var el = document.getElementById('comboEstimate');
        if (!el) return;

        var dto = buildProfileDto();
        var est = estimateCombinations(dto);

        if (est.axisCount === 0) {
            el.textContent = 'Configure at least one parameter range to sweep.';
            el.style.color = '#8d969e';
        } else {
            el.textContent = '≈ ' + est.total.toLocaleString() + ' combinations to test';
            el.style.color = est.total > 20000 ? '#d98300' : '#505a63';
        }
    }

    document.querySelectorAll('.opt-input, .opt-risk-input').forEach(function (el) {
        el.addEventListener('input', updateEstimate);
    });
    updateEstimate();

    function resetStartButton() {
        if (!startBtn) return;
        startBtn.disabled = false;
        startBtn.textContent = '⚡ Start Optimization';
    }

    function metricCard(value, label, color, small) {
        var pad = small ? '1.5rem' : '2rem';
        var style = color ? ' style="color:' + color + ';"' : '';
        return '<div class="card" style="text-align: center; padding: ' + pad + ';">' +
            '<div class="metric"><div class="metric-value"' + style + '>' + value + '</div>' +
            '<div class="metric-label">' + label + '</div></div></div>';
    }

    // Renders the metric cards + parameter list for a single chosen option.
    function renderOptionDetail(opt) {
        var profitColor = opt.profit >= 0 ? '#00a87e' : '#e23b4a';

        var paramsHtml = (opt.paramDisplay || []).map(function (p) {
            return '<div><div style="font-size: 0.85rem; color: #8d969e;">' + p.label + '</div>' +
                '<div style="font-weight: 600;">' + p.value + '</div></div>';
        }).join('');

        return '<div class="grid grid-4">'
            + metricCard(opt.score.toFixed(1), 'Composite Score')
            + metricCard((opt.profit < 0 ? '-$' : '$') + Math.abs(opt.profit).toFixed(2),
                'Profit/Loss (' + opt.profitPercent.toFixed(2) + '%)', profitColor)
            + metricCard(opt.winRate.toFixed(0) + '%', 'Win Rate')
            + metricCard(opt.totalTrades, 'Total Trades')
            + '</div>'
            + '<div class="grid grid-3" style="margin-top: 1.5rem;">'
            + metricCard(opt.profitFactor.toFixed(2), 'Profit Factor', null, true)
            + metricCard(opt.maxDrawdownPercent.toFixed(2) + '%', 'Max Drawdown', '#e23b4a', true)
            + metricCard(opt.sharpeApprox.toFixed(2), 'Sharpe (approx.)', null, true)
            + '</div>'
            + '<div class="card" style="margin-top: 1.5rem;">'
            + '<h6>Parameters for this option</h6>'
            + '<div class="grid grid-3" style="margin-top: 0.75rem;">' + paramsHtml + '</div>'
            + '</div>';
    }

    // Short one-line summary shown on each selector tab.
    function optionSummary(opt) {
        return (opt.profit < 0 ? '-$' : '$') + Math.abs(opt.profit).toFixed(0)
            + ' · ' + opt.winRate.toFixed(0) + '% win · DD ' + opt.maxDrawdownPercent.toFixed(1) + '%';
    }

    function renderResult(data) {
        currentResult = data;

        if (!data.options || data.options.length === 0) {
            document.getElementById('resultSection').innerHTML =
                '<h3>Optimization Results</h3><div class="card" style="padding:1.5rem;">'
                + '<p style="color:#8d969e;margin:0;">No combination produced any trades. Try widening the ranges.</p></div>';
            return;
        }

        if (!selectedOptionKey || !data.options.some(function (o) { return o.key === selectedOptionKey; })) {
            selectedOptionKey = data.options[0].key;
        }

        var tabsHtml = data.options.map(function (opt) {
            var active = opt.key === selectedOptionKey;
            return '<button type="button" class="opt-option-tab' + (active ? ' active' : '') + '" data-opt-key="' + opt.key + '">'
                + '<span class="opt-option-label">' + opt.label + '</span>'
                + '<span class="opt-option-summary">' + optionSummary(opt) + '</span>'
                + '</button>';
        }).join('');

        var html = '<h3>Optimization Results <small style="color:#8d969e;font-weight:400;">('
            + data.totalCombinations.toLocaleString() + ' combinations tested)</small></h3>'
            + '<p style="color:#505a63;margin:0 0 1rem 0;">Pick the result you want, then apply it to the strategy.</p>'
            + '<div class="opt-option-tabs">' + tabsHtml + '</div>'
            + '<div id="optOptionDetail" style="margin-top:1.5rem;"></div>'
            + '<div class="form-actions" style="margin-top: 1.5rem;">'
            + '<button id="applyBestParamsBtn" type="button" class="btn btn-primary" '
            + 'style="padding: 0.75rem 1.5rem; border-radius: 8px; font-weight: 600;">'
            + 'Apply to Strategy &amp; Re-run Session</button>'
            + '</div>';

        document.getElementById('resultSection').innerHTML = html;
        renderSelectedDetail();
    }

    function renderSelectedDetail() {
        if (!currentResult) return;
        var opt = currentResult.options.filter(function (o) { return o.key === selectedOptionKey; })[0];
        if (!opt) return;
        document.getElementById('optOptionDetail').innerHTML = renderOptionDetail(opt);
    }

    function loadResult(runId) {
        fetch('/api/optimization/' + runId + '/result')
            .then(function (res) { return res.json(); })
            .then(function (data) {
                renderResult(data);
                resetStartButton();
                showToast('Optimization complete.', 'success');
            })
            .catch(function () {
                showToast('Failed to load optimization result.', 'danger');
                resetStartButton();
            });
    }

    function pollStatus(runId) {
        var bar = document.getElementById('progressBar');
        var status = document.getElementById('progressStatus');
        var best = document.getElementById('progressBest');

        pollTimer = setInterval(function () {
            fetch('/api/optimization/' + runId + '/status')
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    var pct = data.total > 0 ? Math.round((data.completed / data.total) * 100) : 0;
                    bar.style.width = pct + '%';
                    status.textContent = 'Testing combination ' + data.completed + ' of ' + data.total + '…';
                    if (data.completed > 0) {
                        best.textContent = 'Best score so far: ' + data.bestScoreSoFar.toFixed(1);
                    }

                    if (data.status === 'completed') {
                        clearInterval(pollTimer);
                        bar.style.width = '100%';
                        status.textContent = 'Completed ' + data.total + ' combinations.';
                        loadResult(runId);
                    } else if (data.status === 'failed') {
                        clearInterval(pollTimer);
                        status.textContent = 'Optimization failed.';
                        showToast(data.errorMessage || 'Optimization failed.', 'danger');
                        resetStartButton();
                    }
                })
                .catch(function () {
                    clearInterval(pollTimer);
                    showToast('Lost connection while checking optimization status.', 'danger');
                    resetStartButton();
                });
        }, 800);
    }

    if (startBtn) {
        startBtn.addEventListener('click', function () {
            var dto = buildProfileDto();
            var est = estimateCombinations(dto);

            if (est.axisCount === 0) {
                showToast('Configure at least one parameter range to sweep.', 'danger');
                return;
            }

            startBtn.disabled = true;
            startBtn.textContent = 'Starting…';
            selectedOptionKey = null;

            fetch('/api/optimization/' + window.optimizationConfig.sessionId + '/start', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            })
                .then(function (res) {
                    return res.json().then(function (body) { return { ok: res.ok, body: body }; });
                })
                .then(function (r) {
                    if (!r.ok) {
                        showToast(r.body.error || 'Failed to start optimization.', 'danger');
                        resetStartButton();
                        return;
                    }

                    document.getElementById('progressSection').style.display = '';
                    document.getElementById('resultSection').innerHTML = '';
                    document.getElementById('progressBar').style.width = '0%';
                    document.getElementById('progressBest').textContent = '';
                    pollStatus(r.body.runId);
                })
                .catch(function () {
                    showToast('Failed to start optimization.', 'danger');
                    resetStartButton();
                });
        });
    }

    // Delegated handlers: the result UI is re-rendered after each completed run.
    document.addEventListener('click', function (e) {
        var tab = e.target.closest('.opt-option-tab');
        if (tab) {
            selectedOptionKey = tab.getAttribute('data-opt-key');
            document.querySelectorAll('.opt-option-tab').forEach(function (t) { t.classList.remove('active'); });
            tab.classList.add('active');
            renderSelectedDetail();
            return;
        }

        var btn = e.target.closest('#applyBestParamsBtn');
        if (!btn) return;

        var opt = currentResult && currentResult.options.filter(function (o) { return o.key === selectedOptionKey; })[0];
        if (!opt) {
            showToast('Select an option to apply.', 'danger');
            return;
        }

        btn.disabled = true;
        btn.textContent = 'Applying…';

        fetch('/api/optimization/' + currentResult.runId + '/apply', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ resultId: opt.resultId })
        })
            .then(function (res) {
                return res.json().then(function (body) { return { ok: res.ok, body: body }; });
            })
            .then(function (r) {
                if (!r.ok) {
                    showToast(r.body.error || 'Failed to apply parameters.', 'danger');
                    btn.disabled = false;
                    btn.textContent = 'Apply to Strategy & Re-run Session';
                    return;
                }
                showToast('Parameters applied and session re-run.', 'success');
                setTimeout(function () {
                    window.location.href = '/backtests/' + window.optimizationConfig.sessionId;
                }, 1000);
            })
            .catch(function () {
                showToast('Failed to apply parameters.', 'danger');
                btn.disabled = false;
                btn.textContent = 'Apply to Strategy & Re-run Session';
            });
    });

    // Render a previously-completed run's result on page load, if present.
    if (window.optimizationConfig && window.optimizationConfig.latestResult) {
        renderResult(window.optimizationConfig.latestResult);
    }
})();
