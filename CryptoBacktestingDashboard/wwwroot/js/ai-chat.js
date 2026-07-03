// ── AI Chat Widget ────────────────────────────────────────────────
(function () {
    'use strict';

    // ── State ──────────────────────────────────────────────────────
    var STORAGE_KEY = 'aiChatHistory';
    var MAX_HISTORY = 100;
    var TYPING_SPEED = 12;    // ms per character — lower = faster
    var messages = loadHistory();
    var isOpen = false;
    var isSending = false;
    var isTyping = false;
    var remainingMessages = -1;
    var isUnlimited = false;

    // ── DOM refs ───────────────────────────────────────────────────
    var toggleBtn = document.getElementById('aiChatToggle');
    var overlay = document.getElementById('aiChatOverlay');
    var panel = document.getElementById('aiChatPanel');
    var messagesEl = document.getElementById('aiChatMessages');
    var inputEl = document.getElementById('aiChatInput');
    var sendBtn = document.getElementById('aiChatSendBtn');
    var closeBtn = document.getElementById('aiChatClose');
    var clearBtn = document.getElementById('aiChatClear');
    var statusEl = document.getElementById('aiChatStatus');
    var welcomeEl = document.getElementById('aiChatWelcome');
    var suggestionEls = document.querySelectorAll('.ai-chat-suggestion-chip');

    // ── Storage helpers ────────────────────────────────────────────
    function loadHistory() {
        try {
            var data = localStorage.getItem(STORAGE_KEY);
            return data ? JSON.parse(data) : [];
        } catch (e) {
            return [];
        }
    }

    function saveHistory() {
        try {
            var trimmed = messages.slice(-MAX_HISTORY);
            localStorage.setItem(STORAGE_KEY, JSON.stringify(trimmed));
        } catch (e) { /* localStorage may be full */ }
    }

    // ── Fetch status ───────────────────────────────────────────────
    function fetchStatus() {
        fetch('/api/chat/status')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                remainingMessages = data.remaining;
                isUnlimited = data.unlimited;
                updateStatusBar();
            })
            .catch(function () { /* ignore */ });
    }

    function updateStatusBar() {
        if (!statusEl) return;
        if (isUnlimited) {
            statusEl.textContent = 'Unlimited messages (Admin)';
            statusEl.className = 'ai-chat-status';
        } else if (remainingMessages >= 0) {
            statusEl.textContent = remainingMessages + ' messages remaining today';
            statusEl.className = 'ai-chat-status' + (remainingMessages <= 5 ? ' ai-chat-status--warning' : '');
        } else {
            statusEl.textContent = 'Checking usage…';
            statusEl.className = 'ai-chat-status';
        }
    }

    // ── Full markdown → HTML ───────────────────────────────────────
    function formatMessage(text) {
        if (!text) return '';

        // Must process code blocks BEFORE escaping anything else
        var blocks = [];
        var codeBlockRegex = /```(\w*)\n?([\s\S]*?)```/g;
        var lastIdx = 0;
        var parts = [];

        text.replace(codeBlockRegex, function (match, lang, code, offset) {
            // text before this code block
            parts.push({ type: 'text', content: text.slice(lastIdx, offset) });
            parts.push({ type: 'code', lang: lang, content: code.trim() });
            lastIdx = offset + match.length;
            return match;
        });
        parts.push({ type: 'text', content: text.slice(lastIdx) });

        var result = '';
        for (var p = 0; p < parts.length; p++) {
            var part = parts[p];
            if (part.type === 'code') {
                result += '<pre><code>' + escapeHtml(part.content) + '</code></pre>';
            } else {
                result += formatInline(part.content);
            }
        }
        return result;
    }

    function formatInline(text) {
        var escaped = escapeHtml(text);

        // Headers (## ...) — must come before <br> conversion
        escaped = escaped.replace(/^#{3}\s+(.+)$/gm, '<h4>$1</h4>');
        escaped = escaped.replace(/^#{2}\s+(.+)$/gm, '<h3>$1</h3>');
        escaped = escaped.replace(/^#\s+(.+)$/gm, '<h2>$1</h2>');

        // Horizontal rules
        escaped = escaped.replace(/^---+\s*$/gm, '<hr>');

        // Bold **text**
        escaped = escaped.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

        // Italic *text*
        escaped = escaped.replace(/\*([^*]+)\*/g, '<em>$1</em>');

        // Inline code `text`
        escaped = escaped.replace(/`([^`]+)`/g, '<code>$1</code>');

        // Tables: | col1 | col2 | ...
        escaped = convertTables(escaped);

        // Newlines → <br>
        escaped = escaped.replace(/\n/g, '<br>');

        return escaped;
    }

    function convertTables(text) {
        // Find table blocks: consecutive lines starting with |
        var lines = text.split('\n');
        var inTable = false;
        var result = [];
        var tableRows = [];

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i].trim();
            if (line.startsWith('|') && line.endsWith('|')) {
                if (!inTable) {
                    inTable = true;
                    tableRows = [];
                }
                tableRows.push(line);
            } else {
                if (inTable) {
                    result.push(buildTableHtml(tableRows));
                    inTable = false;
                }
                result.push(lines[i]);
            }
        }
        if (inTable) {
            result.push(buildTableHtml(tableRows));
        }
        return result.join('\n');
    }

    function buildTableHtml(rows) {
        // Identify separator rows (e.g. |---|---| — all cells are dashes/colons)
        function isSeparatorRow(row) {
            var cells = row.split('|').slice(1, -1);
            if (cells.length === 0) return false;
            return cells.every(function (c) {
                return /^[\s\-:]+$/.test(c.trim());
            });
        }

        var dataRows = [];
        var headerRow = null;
        for (var i = 0; i < rows.length; i++) {
            if (isSeparatorRow(rows[i])) continue;
            if (headerRow === null) {
                headerRow = rows[i];
            } else {
                dataRows.push(rows[i]);
            }
        }

        function parseCells(row) {
            return row.split('|').slice(1, -1).map(function (c) { return c.trim(); });
        }

        var html = '<div class="ai-chat-table-wrap"><table class="ai-chat-table">';
        if (headerRow) {
            html += '<thead><tr>';
            parseCells(headerRow).forEach(function (cell) {
                html += '<th>' + cell + '</th>';
            });
            html += '</tr></thead>';
        }
        if (dataRows.length > 0) {
            html += '<tbody>';
            dataRows.forEach(function (row) {
                html += '<tr>';
                parseCells(row).forEach(function (cell) {
                    html += '<td>' + cell + '</td>';
                });
                html += '</tr>';
            });
            html += '</tbody>';
        }
        html += '</table></div>';
        return html;
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ── Render (full rebuild, no animation) ────────────────────────
    function renderMessages() {
        if (!messagesEl) return;

        if (welcomeEl) {
            welcomeEl.style.display = messages.length === 0 ? '' : 'none';
        }

        if (messages.length === 0) {
            messagesEl.innerHTML = '';
            return;
        }

        var html = '';
        for (var i = 0; i < messages.length; i++) {
            var m = messages[i];
            var cls = 'ai-chat-message ai-chat-message--' + m.role;
            html += '<div class="' + cls + '">' + formatMessage(m.content) + '</div>';
        }
        messagesEl.innerHTML = html;
        scrollToBottom();
    }

    function scrollToBottom() {
        if (messagesEl) {
            messagesEl.scrollTop = messagesEl.scrollHeight;
        }
    }

    function showTyping() {
        if (!messagesEl) return;
        var el = document.createElement('div');
        el.className = 'ai-chat-typing';
        el.id = 'aiChatTyping';
        el.innerHTML = '<span></span><span></span><span></span>';
        messagesEl.appendChild(el);
        scrollToBottom();
    }

    function hideTyping() {
        var el = document.getElementById('aiChatTyping');
        if (el && el.parentElement) el.parentElement.removeChild(el);
    }

    // ── Typewriter animation ──────────────────────────────────────
    function typeMessageIntoContainer(container, html, speed, doneCallback) {
        isTyping = true;
        container.innerHTML = '';
        container.style.visibility = 'visible';

        // Break HTML into atomic chunks (tags → whole tag, text → 1 char each)
        var chunks = tokenizeHtmlForTyping(html);
        var idx = 0;

        function typeNext() {
            if (idx >= chunks.length) {
                isTyping = false;
                if (doneCallback) doneCallback();
                return;
            }

            var chunk = chunks[idx];
            container.innerHTML += chunk;
            idx++;
            scrollToBottom();

            var delay = speed;
            // Pause a bit longer on punctuation
            var lastChar = chunk.length === 1 ? chunk : '';
            if (lastChar === '.' || lastChar === '!' || lastChar === '?') delay = speed * 8;
            else if (lastChar === ',' || lastChar === ';' || lastChar === ':') delay = speed * 4;
            else if (lastChar === '\n' || chunk === '<br>') delay = speed * 6;

            setTimeout(typeNext, delay);
        }

        typeNext();
    }

    function tokenizeHtmlForTyping(html) {
        var chunks = [];
        var i = 0;

        while (i < html.length) {
            // HTML tag
            if (html[i] === '<') {
                var end = html.indexOf('>', i);
                if (end !== -1) {
                    chunks.push(html.substring(i, end + 1));
                    i = end + 1;
                    continue;
                }
            }
            // HTML entity (&amp; etc.)
            if (html[i] === '&') {
                var semi = html.indexOf(';', i);
                if (semi !== -1 && semi - i <= 8) {
                    chunks.push(html.substring(i, semi + 1));
                    i = semi + 1;
                    continue;
                }
            }
            // Single character
            chunks.push(html[i]);
            i++;
        }
        return chunks;
    }

    // ── Tool-call categorisation ─────────────────────────────────
    // Maps a tool name prefix to a visual category, an action verb and an icon.
    var TOOL_ICONS = {
        read:   '<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>',
        create: '<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="16"></line><line x1="8" y1="12" x2="16" y2="12"></line></svg>',
        del:    '<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>',
        run:    '<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon></svg>'
    };
    var FAIL_ICON = '<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>';
    var SPINNER_ICON = '<svg xmlns="http://www.w3.org/2000/svg" class="ai-chat-spinner" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M21 12a9 9 0 1 1-6.219-8.56"></path></svg>';

    function classifyTool(tool) {
        var t = (tool || '').toLowerCase();
        if (t.indexOf('list_') === 0 || t.indexOf('get_') === 0) return { cat: 'read', cls: 'read', verb: 'Read', gerund: 'Reading' };
        if (t.indexOf('create_') === 0) return { cat: 'create', cls: 'create', verb: 'Created', gerund: 'Creating' };
        if (t.indexOf('add_') === 0) return { cat: 'create', cls: 'create', verb: 'Added', gerund: 'Adding' };
        if (t.indexOf('delete_') === 0) return { cat: 'del', cls: 'delete', verb: 'Deleted', gerund: 'Deleting' };
        if (t.indexOf('run_') === 0) return { cat: 'run', cls: 'run', verb: 'Ran', gerund: 'Running' };
        if (t.indexOf('fetch_') === 0) return { cat: 'run', cls: 'run', verb: 'Fetched', gerund: 'Fetching' };
        if (t.indexOf('update_') === 0) return { cat: 'create', cls: 'create', verb: 'Updated', gerund: 'Updating' };
        return { cat: 'read', cls: 'read', verb: 'Did', gerund: 'Working' };
    }

    // A chip in the "running" state (live spinner) — shown when a tool starts.
    function makeRunningChip(tool, index) {
        var info = classifyTool(tool);
        var el = document.createElement('div');
        el.className = 'ai-chat-tool-call ai-chat-tool-call--' + info.cls + ' is-running';
        el.style.animationDelay = '0ms';
        el.innerHTML = '<div class="ai-chat-tool-call-header">' + SPINNER_ICON +
            '<span class="verb">' + info.gerund + '</span>' +
            '<span class="noun">' + escapeHtml(nounFromTool(tool)) + '</span>' +
            '<span class="status">…</span>' +
            '</div>';
        return el;
    }

    // Flip a running chip to its finished (done/failed) state.
    function finishChip(el, tool, success) {
        var info = classifyTool(tool);
        var icon = success ? (TOOL_ICONS[info.cat] || TOOL_ICONS.read) : FAIL_ICON;
        el.className = 'ai-chat-tool-call ai-chat-tool-call--' + (success ? info.cls : 'error');
        el.innerHTML = '<div class="ai-chat-tool-call-header">' + icon +
            '<span class="verb">' + (success ? info.verb : 'Failed') + '</span>' +
            '<span class="noun">' + escapeHtml(nounFromTool(tool)) + '</span>' +
            '<span class="status">' + (success ? 'done' : 'failed') + '</span>' +
            '</div>';
    }

    // Mutations should trigger a live refresh of whatever list the user is viewing.
    function isMutatingTool(tool) {
        var info = classifyTool(tool);
        return info.cat === 'create' || info.cat === 'del' || info.cat === 'run';
    }

    function nounFromTool(tool) {
        var us = (tool || '').indexOf('_');
        var noun = us === -1 ? tool : tool.slice(us + 1);
        return noun.replace(/_/g, ' ');
    }

    // ── Render a tool execution chip ─────────────────────────────
    function renderToolCall(tc, index) {
        var info = classifyTool(tc.tool);
        var icon = tc.success ? (TOOL_ICONS[info.cat] || TOOL_ICONS.read) : FAIL_ICON;
        var cls = tc.success ? info.cls : 'error';
        var verb = tc.success ? info.verb : 'Failed';
        var delay = (index || 0) * 130;

        return '<div class="ai-chat-tool-call ai-chat-tool-call--' + cls + '" style="animation-delay:' + delay + 'ms">' +
            '<div class="ai-chat-tool-call-header">' +
            icon +
            '<span class="verb">' + verb + '</span>' +
            '<span class="noun">' + escapeHtml(nounFromTool(tc.tool)) + '</span>' +
            '<span class="status">' + (tc.success ? 'done' : 'failed') + '</span>' +
            '</div>' +
            '</div>';
    }

    // ── Send message ──────────────────────────────────────────────
    function sendMessage(text) {
        if (isSending || isTyping) return;
        if (!text || text.trim() === '') return;

        if (!isUnlimited && remainingMessages === 0) {
            showError('You have used all your daily messages. Limit resets at midnight UTC.');
            return;
        }

        var trimmed = text.trim();

        // Add user message (instant render)
        messages.push({ role: 'user', content: trimmed });
        saveHistory();
        renderMessages();

        // Clear input
        inputEl.value = '';
        inputEl.style.height = 'auto';
        sendBtn.disabled = true;

        // Show typing dots
        isSending = true;
        showTyping();

        var history = messages.map(function (m) {
            return { role: m.role, content: m.content };
        });

        // ── Streamed response state (built up as SSE events arrive) ──
        var bubble = null;
        var toolsWrap = null;
        var replyEl = null;
        var chipEls = {};      // tool index → chip element
        var mutated = false;
        var finished = false;

        function ensureBubble() {
            if (bubble) return;
            hideTyping();
            bubble = document.createElement('div');
            bubble.className = 'ai-chat-message ai-chat-message--assistant';
            messagesEl.appendChild(bubble);
        }

        function ensureToolsWrap() {
            ensureBubble();
            if (!toolsWrap) {
                toolsWrap = document.createElement('div');
                toolsWrap.className = 'ai-chat-tool-calls';
                bubble.appendChild(toolsWrap);
            }
        }

        function finishSending() {
            if (finished) return;
            finished = true;
            isSending = false;
            if (sendBtn) sendBtn.disabled = false;
            if (mutated) setTimeout(refreshCurrentList, 250);
            fetchStatus();
        }

        function handleEvent(evt) {
            if (!evt || !evt.type) return;

            if (evt.type === 'tool_start') {
                ensureToolsWrap();
                var chip = makeRunningChip(evt.tool, evt.index);
                chipEls[evt.index] = chip;
                toolsWrap.appendChild(chip);
                scrollToBottom();
            } else if (evt.type === 'tool_end') {
                var el = chipEls[evt.index];
                if (el) finishChip(el, evt.tool, evt.success);
                if (evt.success && isMutatingTool(evt.tool)) mutated = true;
                scrollToBottom();
            } else if (evt.type === 'reply') {
                ensureBubble();
                var reply = evt.reply || '';
                messages.push({ role: 'assistant', content: reply });
                saveHistory();
                replyEl = document.createElement('div');
                replyEl.className = 'ai-chat-reply';
                bubble.appendChild(replyEl);
                typeMessageIntoContainer(replyEl, formatMessage(reply), TYPING_SPEED, function () {
                    scrollToBottom();
                });
            } else if (evt.type === 'error') {
                hideTyping();
                showError(evt.error || 'Something went wrong.');
            } else if (evt.type === 'done') {
                finishSending();
            }
        }

        fetch('/api/chat/stream', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: trimmed, history: history })
        })
            .then(function (resp) {
                if (!resp.ok || !resp.body) {
                    throw new Error('stream unavailable');
                }
                var reader = resp.body.getReader();
                var decoder = new TextDecoder();
                var buffer = '';

                function pump() {
                    return reader.read().then(function (res) {
                        if (res.done) {
                            finishSending();
                            return;
                        }
                        buffer += decoder.decode(res.value, { stream: true });

                        // SSE frames are separated by a blank line.
                        var frames = buffer.split('\n\n');
                        buffer = frames.pop();
                        frames.forEach(function (frame) {
                            var line = frame.trim();
                            if (line.indexOf('data:') !== 0) return;
                            var payload = line.slice(5).trim();
                            if (!payload) return;
                            try { handleEvent(JSON.parse(payload)); } catch (e) { /* skip bad frame */ }
                        });
                        return pump();
                    });
                }

                return pump();
            })
            .catch(function () {
                hideTyping();
                finishSending();
                showError('Network error. Please check your connection and try again.');
            });
    }

    // ── Live refresh of the current list page ─────────────────────
    // Every list/index page defines a global loadPage(page) that re-fetches its
    // list partial via AJAX. After the assistant mutates data we call it so newly
    // created/deleted/updated items appear instantly without a manual refresh.
    function refreshCurrentList() {
        try {
            if (typeof window.loadPage === 'function') {
                window.loadPage(1);
            }
        } catch (e) { /* page has no refreshable list */ }
        // Notify any page that wants to react to agent-driven data changes.
        try {
            document.dispatchEvent(new CustomEvent('ai:data-changed'));
        } catch (e) { /* old browser */ }
    }

    function showError(msg) {
        if (!messagesEl) return;
        var el = document.createElement('div');
        el.className = 'ai-chat-message ai-chat-message--error';
        el.textContent = msg;
        messagesEl.appendChild(el);
        scrollToBottom();
    }

    // ── UI controls ────────────────────────────────────────────────
    function openPanel() {
        isOpen = true;
        panel.classList.add('ai-chat-panel--open');
        overlay.classList.add('ai-chat-overlay--open');
        document.body.style.overflow = 'hidden';
        if (inputEl) inputEl.focus();
        fetchStatus();
    }

    function closePanel() {
        isOpen = false;
        panel.classList.remove('ai-chat-panel--open');
        overlay.classList.remove('ai-chat-overlay--open');
        document.body.style.overflow = '';
    }

    function clearConversation() {
        if (messages.length === 0) return;
        if (!confirm('Clear the current conversation?')) return;
        messages = [];
        saveHistory();
        renderMessages();
        fetchStatus();
    }

    function autoResize(el) {
        el.style.height = 'auto';
        el.style.height = Math.min(el.scrollHeight, 100) + 'px';
    }

    // ── Init ───────────────────────────────────────────────────────
    function init() {
        renderMessages();

        if (toggleBtn) {
            toggleBtn.addEventListener('click', openPanel);
        }

        if (overlay) {
            overlay.addEventListener('click', closePanel);
        }

        if (closeBtn) {
            closeBtn.addEventListener('click', closePanel);
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', clearConversation);
        }

        if (inputEl) {
            inputEl.addEventListener('input', function () {
                autoResize(this);
                sendBtn.disabled = this.value.trim() === '' || isSending || isTyping;
            });

            inputEl.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    sendMessage(this.value);
                }
            });
        }

        if (sendBtn) {
            sendBtn.addEventListener('click', function () {
                sendMessage(inputEl ? inputEl.value : '');
            });
        }

        suggestionEls.forEach(function (chip) {
            chip.addEventListener('click', function () {
                var text = this.getAttribute('data-prompt') || this.textContent;
                if (!isOpen) openPanel();
                setTimeout(function () {
                    if (inputEl) {
                        inputEl.value = text;
                        autoResize(inputEl);
                        sendBtn.disabled = false;
                        inputEl.focus();
                    }
                }, 400);
            });
        });

        // Keyboard shortcut: Ctrl+Shift+A
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'a') {
                e.preventDefault();
                if (isOpen) closePanel();
                else openPanel();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
