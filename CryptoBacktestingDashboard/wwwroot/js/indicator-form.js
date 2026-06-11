// Adapts the Indicator Create/Edit form to the selected indicator type.
// The DB stores only generic `Period` and `Threshold` columns, but they mean
// different things per type — this rewrites the labels/help so it's obvious
// what to enter, hides the Threshold when a type doesn't use it, and applies
// sensible validation ranges.
(function () {
    "use strict";

    // null threshold => the type ignores Threshold (field is hidden).
    var CONFIG = {
        RSI: {
            period: { label: "Period (candles)", help: "Lookback window for RSI. Standard = 14." },
            threshold: { label: "Overbought level", help: "Sell above this; buy below (100 − level). Standard = 70.", min: 1, max: 99 },
        },
        MACD: {
            period: { label: "Fast EMA period", help: "Fast EMA length. Standard = 12." },
            threshold: { label: "Slow EMA period", help: "Slow EMA length (entered here). Standard = 26. Signal EMA is fixed at 9.", min: 2, max: 200 },
        },
        MovingAverage: {
            period: { label: "MA length (candles)", help: "Length of the moving average, e.g. 20 or 50." },
            threshold: null,
        },
        BollingerBands: {
            period: { label: "SMA length (candles)", help: "Length of the middle-band SMA. Standard = 20." },
            threshold: { label: "Std-dev multiplier", help: "Band width in standard deviations. Standard = 2.", min: 0.5, max: 5 },
        },
        Stochastic: {
            period: { label: "%K length (candles)", help: "Lookback for %K. Standard = 14." },
            threshold: { label: "Overbought level", help: "Sell above this; buy below (100 − level). Standard = 80.", min: 1, max: 99 },
        },
        ATR: {
            period: { label: "Period (candles)", help: "Lookback for ATR. Standard = 14." },
            threshold: null,
            note: "ATR measures volatility only — on its own it does not generate buy/sell signals.",
        },
    };

    function apply() {
        var typeEl = document.getElementById("Type");
        if (!typeEl) return;

        var cfg = CONFIG[typeEl.value];
        var periodLabel = document.getElementById("periodLabel");
        var periodHelp = document.getElementById("periodHelp");
        var thresholdGroup = document.getElementById("thresholdGroup");
        var thresholdLabel = document.getElementById("thresholdLabel");
        var thresholdHelp = document.getElementById("thresholdHelp");
        var thresholdInput = document.getElementById("Threshold");
        var typeNote = document.getElementById("typeNote");

        if (!cfg) {
            if (typeNote) typeNote.textContent = "";
            return;
        }

        if (periodLabel) periodLabel.textContent = cfg.period.label;
        if (periodHelp) periodHelp.textContent = cfg.period.help;

        if (cfg.threshold) {
            if (thresholdGroup) thresholdGroup.style.display = "";
            if (thresholdLabel) thresholdLabel.textContent = cfg.threshold.label;
            if (thresholdHelp) thresholdHelp.textContent = cfg.threshold.help;
            if (thresholdInput) {
                if (cfg.threshold.min != null) thresholdInput.min = cfg.threshold.min;
                else thresholdInput.removeAttribute("min");
                if (cfg.threshold.max != null) thresholdInput.max = cfg.threshold.max;
                else thresholdInput.removeAttribute("max");
            }
        } else if (thresholdGroup) {
            thresholdGroup.style.display = "none";
        }

        if (typeNote) typeNote.textContent = cfg.note || "";
    }

    document.addEventListener("DOMContentLoaded", function () {
        var typeEl = document.getElementById("Type");
        if (!typeEl) return;
        typeEl.addEventListener("change", apply);
        apply(); // run once for the pre-selected value (Edit form / validation re-render)
    });
})();
