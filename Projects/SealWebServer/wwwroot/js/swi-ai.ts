/// <reference path="typings/jquery/JQuery.d.ts" />
/// <reference path="typings/main.d.ts" />

// AI panel
(function ($: JQueryStatic) {
    var $panel    = $('#ai-chat-panel');
    var $toggle   = $('#ai-panel-toggle');
    var $close    = $('#ai-panel-close');
    var $newBtn   = $('#ai-panel-new');
    var $input    = $('#ai-panel-input');
    var $send     = $('#ai-panel-send');
    var $messages = $('#ai-panel-messages');
    var $favBtn      = $('#ai-panel-fav-btn');
    var $histBtn     = $('#ai-panel-history-btn');
    var $histDrop    = $('#ai-panel-history-dropdown');
    var $histWrap    = $histBtn.parent(); // .ai-panel-dropdown-wrap
    var $samplesBtn  = $('#ai-panel-samples-btn');
    var $samplesDrop = $('#ai-panel-samples-dropdown');
    var $samplesWrap = $samplesBtn.parent(); // .ai-panel-samples-wrap
    var $agentSelect = $('#ai-panel-agent-select');
    // Move dropdown to <body> so position:fixed is relative to the viewport,
    // not the transformed #ai-chat-panel (transform creates a new stacking context
    // that traps fixed-position descendants).
    $samplesDrop.appendTo('body');

    // ── File name helper ────────────────────────────────────────
    function sanitizeFileName(s: string): string {
        return s.replace(/[\\/:*?"<>|]/g, ' ').replace(/\s+/g, ' ').trim().substring(0, 60) || 'chat';
    }

    // ── Response formatter ──────────────────────────────────────
    function attr(s: string): string {
        return (s || '').replace(/"/g, '&quot;');
    }

    // Markdown table helpers: a table is a |...| header row followed by a |---|---|
    // separator row, then any number of |...| body rows.
    function isTableRow(line: string): boolean {
        const t = line.trim();
        return t.length > 1 && t.charAt(0) === '|' && t.indexOf('|', 1) > 0;
    }
    function isTableSeparator(line: string): boolean {
        return /^\|(\s*:?-+:?\s*\|)+\s*$/.test(line.trim());
    }
    function splitTableCells(line: string): string[] {
        return line.trim().replace(/^\|/, '').replace(/\|$/, '').split('|')
            .map(function (c) { return c.trim(); });
    }
    function tableCellAlign(sep: string): string {
        const left = sep.charAt(0) === ':', right = sep.charAt(sep.length - 1) === ':';
        if (left && right) return 'center';
        if (right) return 'right';
        return '';
    }

    function formatResponse(text: string): string {
        if (!text) return '';
        // Escape HTML to prevent XSS (including quotes, so escaped text is safe in attribute contexts too)
        text = text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');

        // Inline formatting applied within a single (already escaped) line.
        function inline(s: string): string {
            // `inline code`
            s = s.replace(/`([^`\r\n]+)`/g, '<code>$1</code>');
            // **bold**
            s = s.replace(/\*\*([^*\r\n]+)\*\*/g, '<strong>$1</strong>');
            return s;
        }

        // Render a parsed markdown table with a hover toolbar (copy for Excel, download CSV).
        function renderTable(header: string[], aligns: string[], body: string[][]): string {
            const cell = function (tag: string, value: string, col: number): string {
                const a = aligns[col] ? ' style="text-align:' + aligns[col] + '"' : '';
                return '<' + tag + a + '>' + inline(value) + '</' + tag + '>';
            };
            let t = '<div class="ai-table-wrap">';
            t += '<div class="ai-table-actions">';
            t += '<button class="ai-bubble-action-btn ai-table-btn ai-table-copy" title="' + attr(SWIUtil.tr2('Copy for Excel')) + '"><i class="fa fa-copy"></i></button>';
            t += '<button class="ai-bubble-action-btn ai-table-btn ai-table-csv" title="' + attr(SWIUtil.tr2('Download CSV')) + '"><i class="fa fa-download"></i></button>';
            t += '</div>';
            t += '<div class="ai-table-scroll"><table class="ai-table"><thead><tr>';
            for (let c = 0; c < header.length; c++) t += cell('th', header[c], c);
            t += '</tr></thead><tbody>';
            for (const row of body) {
                t += '<tr>';
                for (let c = 0; c < header.length; c++) t += cell('td', row[c] || '', c);
                t += '</tr>';
            }
            t += '</tbody></table></div></div>';
            return t;
        }

        const lines = text.split(/\r\n|\r|\n/);
        let html = '';
        let inList = false;
        const closeList = function () { if (inList) { html += '</ul>'; inList = false; } };

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            // | header | row | followed by a |---|---| separator row
            if (isTableRow(line) && i + 1 < lines.length && isTableSeparator(lines[i + 1])) {
                closeList();
                const header = splitTableCells(line);
                const aligns = splitTableCells(lines[i + 1]).map(tableCellAlign);
                const body: string[][] = [];
                i += 2;
                while (i < lines.length && isTableRow(lines[i])) { body.push(splitTableCells(lines[i])); i++; }
                i--;
                html += renderTable(header, aligns, body);
                continue;
            }
            // # .. ###### headings
            const h = /^(#{1,6})\s+(.*)$/.exec(line);
            if (h) {
                closeList();
                html += '<div class="ai-h ai-h' + h[1].length + '">' + inline(h[2]) + '</div>';
                continue;
            }
            // - or * bullet lists
            const li = /^\s*[-*]\s+(.*)$/.exec(line);
            if (li) {
                if (!inList) { html += '<ul class="ai-list">'; inList = true; }
                html += '<li>' + inline(li[1]) + '</li>';
                continue;
            }
            closeList();
            html += line.trim() === '' ? '<br>' : inline(line) + '<br>';
        }
        closeList();
        return html;
    }

    // ── Execute-report button factory ───────────────────────────
    function makeExecuteButton(path: string, name: string, outputGUID: string): JQuery {
        return $('<button>')
            .addClass('ai-panel-execute-btn')
            .html('<i class="fa fa-play"></i> ' + $('<span>').text(name).html())
            .on('click', function () { _gateway.ExecuteReport(path, '', outputGUID || ''); });
    }

    // ── Report-action parser ────────────────────────────────────
    // Strips [EXECUTE_REPORT:path|name] tags from text, returns cleaned text
    // and a jQuery element (possibly empty) containing Execute buttons.
    function parseReportActions(text: string): { cleaned: string; $actions: JQuery } {
        const $actions = $('<div>').addClass('ai-panel-report-actions');
        const re = /\[EXECUTE_REPORT:([^\]\|]+)\|([^\]\|]+)(?:\|([^\]]*))?\]/g;
        const cleaned = text.replace(re, function (_match: string, rawPath: string, name: string, outputGUID: string) {
            let swiPath: string;
            if (/^Reports[\\\/]/.test(rawPath))
                swiPath = rawPath.substring('Reports'.length);
            else if (/^Personal[\\\/]/.test(rawPath))
                swiPath = ':' + rawPath.substring('Personal'.length);
            else
                swiPath = rawPath;
            makeExecuteButton(swiPath, name.trim(), outputGUID).appendTo($actions);
            return '';
        });
        return { cleaned: cleaned.trim(), $actions };
    }

    // ── Clipboard helper ────────────────────────────────────────
    function fallbackCopy(text: string): void {
        var ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.top = '-1000px';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        try { document.execCommand('copy'); } catch (e) { /* ignore */ }
        document.body.removeChild(ta);
    }
    function copyToClipboard(text: string): void {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).catch(function () { fallbackCopy(text); });
        } else {
            fallbackCopy(text);
        }
    }

    // ── Markdown → plain text (for copying AI replies) ──────────
    function toPlainText(raw: string): string {
        return (raw || '')
            .replace(/^#{1,6}\s+/gm, '')          // strip heading markers
            .replace(/\*\*([^*\r\n]+)\*\*/g, '$1') // strip bold markers
            .replace(/`([^`\r\n]+)`/g, '$1');      // strip inline-code markers
    }

    // ── Table export (copy for Excel / download CSV) ────────────
    // Reads back the rendered table so the export matches exactly what is displayed.
    // '—'/'–' placeholders the model uses for missing values become empty cells.
    function tableToRows($table: JQuery): string[][] {
        const rows: string[][] = [];
        $table.find('tr').each(function () {
            const row: string[] = [];
            $(this).children('th,td').each(function () {
                let v = $(this).text().trim();
                if (v === '—' || v === '–') v = '';
                row.push(v);
            });
            rows.push(row);
        });
        return rows;
    }

    // Excel's own rule: locales using a decimal comma expect ';' as the CSV separator.
    // The Seal profile language drives the choice, falling back to the browser locale.
    function csvSeparator(): string {
        const locale = (typeof languageName !== 'undefined' && languageName) ? languageName : (navigator.language || 'en');
        try {
            return new Intl.NumberFormat(locale).format(1.1).indexOf(',') >= 0 ? ';' : ',';
        } catch (e) {
            return ',';
        }
    }

    function rowsToCsv(rows: string[][], sep: string): string {
        return rows.map(function (r) {
            return r.map(function (v) {
                return (v.indexOf(sep) >= 0 || v.indexOf('"') >= 0 || /[\r\n]/.test(v)) ? '"' + v.replace(/"/g, '""') + '"' : v;
            }).join(sep);
        }).join('\r\n');
    }

    function rowsToTsv(rows: string[][]): string {
        return rows.map(function (r) {
            return r.map(function (v) { return v.replace(/[\t\r\n]+/g, ' '); }).join('\t');
        }).join('\n');
    }

    function downloadCsv(csv: string, fileName: string): void {
        // UTF-8 BOM so Excel opens accented characters correctly
        const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(function () { URL.revokeObjectURL(a.href); }, 1000);
    }

    // Delegated so it also works for bubbles restored from a saved chat.
    $messages.on('click', '.ai-table-btn', function (e: JQuery.ClickEvent) {
        e.stopPropagation();
        const $btn = $(this);
        const rows = tableToRows($btn.closest('.ai-table-wrap').find('table.ai-table'));
        if ($btn.hasClass('ai-table-copy')) {
            copyToClipboard(rowsToTsv(rows));
        }
        else {
            downloadCsv(rowsToCsv(rows, csvSeparator()), sanitizeFileName(SWIUtil.tr2('AI Agent')) + '.csv');
        }
    });

    // ── Bubble factories with hover action toolbars ─────────────
    function bubbleActionBtn(iconClass: string, title: string, handler: () => void): JQuery {
        return $('<button>')
            .addClass('ai-bubble-action-btn')
            .attr('title', title)
            .html('<i class="fa ' + iconClass + '"></i>')
            .on('click', function (e: JQuery.ClickEvent) {
                e.stopPropagation();
                handler();
            });
    }

    function makeUserBubble(text: string): JQuery {
        var $bubble  = $('<div>').addClass('ai-panel-bubble user').text(text);
        var $actions = $('<div>').addClass('ai-bubble-actions');
        bubbleActionBtn('fa-copy', SWIUtil.tr2('Copy'), function () {
            copyToClipboard(text);
        }).appendTo($actions);
        bubbleActionBtn('fa-clipboard', SWIUtil.tr2('Copy and paste'), function () {
            copyToClipboard(text);
            $input.val(text).trigger('input');
            $input.focus();
        }).appendTo($actions);
        bubbleActionBtn('fa-paper-plane', SWIUtil.tr2('Copy, paste and send'), function () {
            copyToClipboard(text);
            $input.val(text).trigger('input');
            if (!$send.prop('disabled')) $send.trigger('click');
        }).appendTo($actions);
        // Rewind: drop this user turn and everything after it, then refill the input
        // so the message can be edited and re-sent.
        bubbleActionBtn('fa-undo', SWIUtil.tr2('Rewind to here'), function () {
            rewindToBubble($bubble, text);
        }).appendTo($actions);
        return $bubble.append($actions);
    }

    function makeAIBubble(html: string, rawText: string): JQuery {
        var $bubble  = $('<div>').addClass('ai-panel-bubble ai').html(html);
        var $actions = $('<div>').addClass('ai-bubble-actions');
        bubbleActionBtn('fa-copy', SWIUtil.tr2('Copy'), function () {
            copyToClipboard(toPlainText(rawText));
        }).appendTo($actions);
        return $bubble.append($actions);
    }

    // ── Rewind conversation to a user bubble ────────────────────
    // Removes the selected user turn and everything after it (both in the UI and in the
    // server-side agent history), then puts the message text back into the input box so it
    // can be edited and re-sent. The bubble's position among the user bubbles maps 1:1 to
    // the agent's UserChatMessage list, so we send that 0-based index to the server.
    function rewindToBubble($bubble: JQuery, text: string): void {
        if (_requesting) return;
        var index = $bubble.prevAll('.ai-panel-bubble.user').length;
        _gateway.RewindAIAgent(index, function () {
            $bubble.nextAll().remove();
            $bubble.remove();
            $input.val(text).trigger('input');
            $input.focus();
            // Keep the saved chat in sync with the truncated conversation.
            if (_panelChatFileName) {
                _gateway.SaveAIAgentChat(_panelChatFileName, _panelChatInfos, function (saved: any) {
                    if (saved && saved.fileName) _panelChatFileName = saved.fileName;
                }, undefined, false);
            }
        });
    }

    // ── Clear conversation (UI only) ────────────────────────────
    function clearConversation(): void {
        $messages.empty();
        $input.val('').trigger('input');
    }

    // ── Panel open / close ──────────────────────────────────────
    function openPanel(): void {
        $panel.addClass('ai-panel-open');
        $toggle.addClass('ai-panel-open').attr('title', SWIUtil.tr2('Hide AI Agent'));
        $('body').addClass('ai-panel-visible');
    }

    function closePanel(): void {
        $panel.removeClass('ai-panel-open');
        $toggle.removeClass('ai-panel-open').attr('title', SWIUtil.tr2('AI Agent'));
        $('body').removeClass('ai-panel-visible');
        closeDropdown();
    }

    $toggle.on('click', function () {
        $panel.hasClass('ai-panel-open') ? closePanel() : openPanel();
    });

    $close.on('click', closePanel);

    // ── Panel resize (drag handle) ──────────────────────────────
    var MIN_PANEL_WIDTH = 300;
    var MAX_PANEL_WIDTH = 1200;
    var $resizer = $('#ai-panel-resizer');

    function applyPanelWidth(px: number): void {
        var clamped = Math.max(MIN_PANEL_WIDTH, Math.min(MAX_PANEL_WIDTH, px));
        document.documentElement.style.setProperty('--ai-panel-width', clamped + 'px');
    }

    // Restore the saved width on load.
    try {
        var saved = window.localStorage.getItem('ai-panel-width');
        if (saved) applyPanelWidth(parseInt(saved, 10));
    } catch (e) { /* localStorage unavailable */ }

    $resizer.on('mousedown', function (e: JQuery.MouseDownEvent) {
        e.preventDefault();
        $('body').addClass('ai-resizing');

        function onMove(ev: MouseEvent): void {
            applyPanelWidth(ev.clientX);
        }
        function onUp(): void {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            $('body').removeClass('ai-resizing');
            var current = getComputedStyle(document.documentElement)
                .getPropertyValue('--ai-panel-width').trim();
            try { window.localStorage.setItem('ai-panel-width', String(parseInt(current, 10))); }
            catch (err) { /* localStorage unavailable */ }
        }
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    });

    // ── Agent selector ──────────────────────────────────────
    // Fetches the agents available to the current user. Shows the selectpicker
    // in the header always (≥ 1 agent). When only one is available the control
    // is disabled so it acts as a plain title label.
    var _agentsLoaded: boolean = false;

    function loadAgents(): void {
        if (_agentsLoaded) return;
        _agentsLoaded = true;
        _gateway.GetUserAgents(function (data: any) {
            var agents: Array<{ guid: string; name: string; description: string }> = data.agents || [];
            if (agents.length === 0) return;

            // Rebuild the options.
            ($agentSelect as any).selectpicker('destroy');
            $agentSelect.empty();
            $.each(agents, function (_, a: { guid: string; name: string; description: string }) {
                $('<option>').val(a.guid).text(a.name).appendTo($agentSelect);
            });

            // Disable when there is only one choice — serves as a read-only title.
            $agentSelect.prop('disabled', agents.length === 1);

            // Select the current agent on the underlying <select> BEFORE building the
            // picker. bootstrap-select 1.14 buildData() appends to its internal data
            // model, so a separate selectpicker('refresh') after init would list every
            // agent twice. Set the value first, then initialise the picker exactly once.
            // Always fall back to the first agent so the picker never sits on the empty
            // "Select agent" placeholder.
            var selectedGuid = data.selectedGuid;
            if (!selectedGuid || !agents.some(function (a) { return a.guid === selectedGuid; })) {
                selectedGuid = agents[0].guid;
                // Persist the default selection on the server so the next request uses it.
                _gateway.SelectAgent(selectedGuid, function () { });
            }
            $agentSelect.val(selectedGuid);

            // Apply description tooltips to the dropdown items. bootstrap-select renders
            // the menu rows LAZILY on first open (the <ul> is empty until then), so the
            // items don't exist right after init nor on the 'loaded' event. Attach on the
            // 'shown' event — and re-attach on every open, because bootstrap-select may
            // rebuild the rows (which discards previously attached tooltips). Setting the
            // native title attribute first guarantees a tooltip even if Bootstrap's Tooltip
            // is unavailable; Bootstrap's Tooltip then upgrades it to a styled one and
            // consumes the attribute (getOrCreateInstance keeps re-attaching idempotent).
            $agentSelect.on('shown.bs.select', function () {
                var $items = $agentSelect.closest('.bootstrap-select').find('.dropdown-menu li a.dropdown-item');
                $.each(agents, function (i: number, a: { guid: string; name: string; description: string }) {
                    if (!a.description) return;
                    var el = $items.get(i);
                    if (!el) return;
                    el.setAttribute('title', a.description);
                    (window as any).bootstrap.Tooltip.getOrCreateInstance(el, { title: a.description, placement: 'right', container: 'body', trigger: 'hover' });
                });
            });
            ($agentSelect as any).selectpicker({ width: 'auto' });
            // Force the picker button to display the selection. Setting the native
            // <select>.val() before init is not reliably reflected on the rendered
            // button, so push it through the plugin API too (safe: 'val' selects an
            // existing option, unlike 'refresh' which would re-append the data).
            ($agentSelect as any).selectpicker('val', selectedGuid);
        });
    }

    $agentSelect.on('change', function () {
        var guid = ($agentSelect.val() as string);
        if (!guid) return;
        _gateway.SelectAgent(guid, function () {
            // Clear the conversation: the new agent has no prior context.
            _gateway.ClearAIAgent(function () {
                clearConversation();
                _panelChatFileName = '';
                setFavorite(false);
                // Refresh history lists so they reflect the newly selected agent.
                refreshHistoryLists();
            });
        });
    });

    // Load the agent list the first time the panel is opened.
    $toggle.on('click', function () {
        if ($panel.hasClass('ai-panel-open')) loadAgents();
    });

    // ── Chat persistence state ──────────────────────────────────
    var _panelChatFileName: string = '';
    var _panelChatInfos: { Key: string; Value: string }[] = [{ Key: 'Type', Value: 'ai-panel' }];

    // ── New conversation button ─────────────────────────────────
    $newBtn.on('click', function () {
        _gateway.ClearAIAgent(function () {
            clearConversation();
            _panelChatFileName = '';
            setFavorite(false);
        });
    });

    // ── Send button ─────────────────────────────────────────────
    var _requesting: boolean = false;
    var $currentTyping: JQuery | null = null;   // live "thinking" panel while a request runs
    var _thinkingSteps: string[] = [];          // steps collected for the current request
    var _progressInterval: number | null = null;
    var $running = $('#ai-panel-running');
    var _runningInterval: number | null = null;
    var _runningDots: string[] = ['', '.', '..', '...'];
    var _runningDotIndex: number = 0;

    function setSendMode(): void {
        $send.find('.send-icon').html('&#10148;');
        $send.removeClass('ai-panel-cancel-btn');
        $input.prop('disabled', false);
        $send.prop('disabled', ($input.val() as string).trim() === '');
        $send.attr('title', '');
        if (_runningInterval !== null) {
            window.clearInterval(_runningInterval);
            _runningInterval = null;
        }
        $running.hide();
    }

    function setCancelMode(): void {
        $send.find('.send-icon').html('&#9632;');
        $send.addClass('ai-panel-cancel-btn');
        $input.prop('disabled', true);
        $send.prop('disabled', false);
        $send.attr('title', SWIUtil.tr2('Cancel'));
        _runningDotIndex = 0;
        $running.text(SWIUtil.tr2('Running')).show();
        _runningInterval = window.setInterval(function () {
            _runningDotIndex = (_runningDotIndex + 1) % _runningDots.length;
            $running.text(SWIUtil.tr2('Running') + _runningDots[_runningDotIndex]);
        }, 400);
    }

    // ── "Thinking" progress steps ───────────────────────────────
    // Live panel shown while a request runs: an animated header plus the list of
    // steps (skill loads, tool calls) streamed from the server via polling.
    function makeThinkingPanel(): JQuery {
        var $panel = $('<div>').addClass('ai-panel-thinking');
        $('<div>').addClass('ai-thinking-header')
            .html('<span class="dot"></span><span class="dot"></span><span class="dot"></span>')
            .append($('<span>').addClass('ai-thinking-label').text(SWIUtil.tr2('Thinking') + '...'))
            .appendTo($panel);
        $('<div>').addClass('ai-thinking-steps').appendTo($panel);
        return $panel;
    }

    function addThinkingStep(text: string): void {
        if (!$currentTyping || !text) return;
        // Suppress consecutive duplicates (the model can repeat the same step).
        if (_thinkingSteps.length > 0 && _thinkingSteps[_thinkingSteps.length - 1] === text) return;
        _thinkingSteps.push(text);
        $currentTyping.find('.ai-thinking-steps')
            .append($('<div>').addClass('ai-thinking-step').text(text));
        $messages[0].scrollTop = $messages[0].scrollHeight;
    }

    // Collapsed, persistent summary placed above the final answer ("Thought for N steps").
    function makeStepsSummary(): JQuery | null {
        if (_thinkingSteps.length === 0) return null;
        var steps = _thinkingSteps.slice();
        var $wrap   = $('<div>').addClass('ai-panel-steps');
        var $list   = $('<div>').addClass('ai-steps-list').hide();
        steps.forEach(function (s) { $('<div>').addClass('ai-thinking-step').text(s).appendTo($list); });
        var label   = SWIUtil.tr2('Thought for {0} steps').replace('{0}', String(steps.length));
        var $toggle = $('<div>').addClass('ai-steps-toggle')
            .html('<i class="fas fa-caret-right"></i> ').append($('<span>').text(label));
        $toggle.on('click', function () {
            var visible = $list.is(':visible');
            $list.toggle(!visible);
            $toggle.find('i').toggleClass('fa-caret-right', visible).toggleClass('fa-caret-down', !visible);
        });
        return $wrap.append($toggle).append($list);
    }

    function startProgressPolling(): void {
        stopProgressPolling();
        _progressInterval = window.setInterval(function () {
            _gateway.GetAIAgentProgress(function (data: any) {
                (data.messages as string[]).forEach(addThinkingStep);
            });
        }, 600);
    }

    function stopProgressPolling(): void {
        if (_progressInterval !== null) {
            window.clearInterval(_progressInterval);
            _progressInterval = null;
        }
    }

    $input.on('input', function () {
        if (!_requesting) $send.prop('disabled', ($input.val() as string).trim() === '');
    });

    $input.on('keydown', function (e: JQuery.KeyDownEvent) {
        if (e.key === 'Enter' && !e.altKey) {
            e.preventDefault(); // suppress default newline
            if (!$send.prop('disabled')) $send.trigger('click');
        }
        // Alt+Enter → insert a newline at the cursor position
        if (e.key === 'Enter' && e.altKey) {
            e.preventDefault();
            const el = $input[0] as HTMLTextAreaElement;
            const start = el.selectionStart;
            const end   = el.selectionEnd;
            const val   = el.value;
            el.value = val.substring(0, start) + '\n' + val.substring(end);
            el.selectionStart = el.selectionEnd = start + 1;
            $input.trigger('input'); // re-evaluate send-button state
        }
    });

    $send.on('click', function () {
        // ── Cancel mode: interrupt the in-flight request ──────────
        if (_requesting) {
            _gateway.CancelAIAgentResponse(null, null);
            stopProgressPolling();
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _requesting = false;
            setSendMode();
            const $cancelled = $('<div>').addClass('ai-panel-bubble ai cancelled').text(SWIUtil.tr2('Request cancelled.'));
            $messages.append($cancelled);
            $messages[0].scrollTop = $messages[0].scrollHeight;
            return;
        }

        const message = ($input.val() as string).trim();
        if (!message) return;

        // Append user bubble immediately
        const userBubble = makeUserBubble(message);
        $messages.append(userBubble);
        $messages[0].scrollTop = $messages[0].scrollHeight;

        $input.val('').trigger('input'); // clear + re-evaluate disabled state

        // Live "thinking" panel — animated header that fills with steps as they stream in
        _thinkingSteps = [];
        $currentTyping = makeThinkingPanel();
        $messages.append($currentTyping);
        $messages[0].scrollTop = $messages[0].scrollHeight;

        _requesting = true;
        setCancelMode();
        startProgressPolling();

        _gateway.GetAIAgentResponse(message, function (data: any) {
            stopProgressPolling();
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            // Persist the steps as a collapsed "Thought for N steps" summary above the answer
            const $stepsSummary = makeStepsSummary();
            if ($stepsSummary) $messages.append($stepsSummary);
            if (data.response) {
                const aiBubble = makeAIBubble(formatResponse(data.response || ''), data.response || '');
                $messages.append(aiBubble);
            }
            // Render Execute buttons when the AI proposes running one or more reports
            if (data.reportActions && (data.reportActions as any[]).length > 0) {
                const $actions = $('<div>').addClass('ai-panel-report-actions');
                (data.reportActions as Array<{ path: string; name: string; outputGUID: string }>).forEach(function (action) {
                    makeExecuteButton(action.path, action.name, action.outputGUID).appendTo($actions);
                });
                $messages.append($actions);
            }
            // Muted usage line under the answer: tokens and cost (cost only when configured on the provider)
            if (data.tokens) {
                let usageText = Number(data.tokens).toLocaleString() + ' ' + SWIUtil.tr2('tokens');
                if (data.cost !== null && data.cost !== undefined) {
                    usageText += ' · ' + SWIUtil.tr2('cost') + ' ' + Number(data.cost).toFixed(data.cost < 0.1 ? 4 : 2);
                }
                $messages.append($('<div>').addClass('ai-panel-usage').text(usageText));
            }

            $messages[0].scrollTop = $messages[0].scrollHeight;
            _requesting = false;
            setSendMode();
            $input.focus();
            // Auto-save to Recents after every exchange.
            // On the first save, ask the AI to generate a friendly summary name
            // (the sanitized user message is sent as a fallback).
            var isFirstSave = !_panelChatFileName;
            var saveName = _panelChatFileName || sanitizeFileName(message);
            _gateway.SaveAIAgentChat(saveName, _panelChatInfos, function (saved: any) {
                if (saved && saved.fileName) _panelChatFileName = saved.fileName;
            }, undefined, isFirstSave);
        }, function (_err: any) {
            stopProgressPolling();
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _requesting = false;
            setSendMode();
            $input.focus();
        });
    });

    // ── Favorite star toggle ────────────────────────────────────
    var _isFavorite: boolean = false;

    function setFavorite(state: boolean): void {
        _isFavorite = state;
        $favBtn.find('i').toggleClass('fas', state).toggleClass('far', !state);
        $favBtn.toggleClass('fav-active', state);
        $favBtn.attr('title', state ? SWIUtil.tr2('Remove from favorites') : SWIUtil.tr2('Mark as favorite'));
    }
    $favBtn.on('click', function () {
        if (!_panelChatFileName) return;
        var newState = !_isFavorite;
        setFavorite(newState);
        _gateway.MarkAIAgentChatFavorite(_panelChatFileName, function (data: any) {
            if (data && typeof data.isFavorite !== 'undefined') setFavorite(data.isFavorite);
        });
    });

    // ── Favorites / MRU dropdown ────────────────────────────────
    function closeDropdown(): void { $histDrop.hide(); }
    function toggleDropdown(): void { $histDrop.toggle(); }

    $histBtn.on('click', function () {
        // No stopPropagation — letting the event reach Bootstrap's document handler
        // so navbar dropdowns can still close normally.
        toggleDropdown();
        // Fetch and populate lists whenever the dropdown is opening
        if ($histDrop.is(':visible')) {
            refreshHistoryLists();
        }
    });

    // Close our dropdown on any outside click.
    // We check the target rather than stopping propagation, so Bootstrap's own
    // document-level listener for navbar dropdowns is never blocked.
    $(document).on('click', function (e: JQuery.ClickEvent) {
        if (!$histWrap.is(e.target) && $histWrap.has(e.target as Element).length === 0) {
            closeDropdown();
        }
    });

    // ── Sample prompts button ───────────────────────────────────
    function openSamplesDropdown():  void { $samplesDrop.show(); }
    function closeSamplesDropdown(): void { $samplesDrop.hide(); }

    function positionSamplesDropdown(): void {
        var rect = ($samplesBtn[0] as HTMLElement).getBoundingClientRect();
        var dropW = 600;
        // Anchor the dropdown's bottom just above the button; it grows upward
        // (avoids a gap when the content is shorter than the max-height).
        var bottom = window.innerHeight - rect.top + 6;
        // Open to the right; clamp left so it stays inside the viewport
        var left = rect.right + 6;
        if (left + dropW > window.innerWidth - 8) left = window.innerWidth - dropW - 8;
        $samplesDrop.css({ bottom: bottom + 'px', left: left + 'px', right: 'auto', top: 'auto' });
    }

    $samplesBtn.on('click', function () {
        if ($samplesDrop.is(':visible')) {
            closeSamplesDropdown();
            return;
        }
        _gateway.GetAIAgentSamplePrompts(function (data: any) {
            var $list = $('#ai-panel-samples-list');
            $list.empty();
            var prompts: string[] = data.prompts || [];
            if (prompts.length === 0) {
                $list.append('<li class="ai-panel-dropdown-empty">' + SWIUtil.tr('No sample prompts defined') + '</li>');
            } else {
                $.each(prompts, function (_, prompt: string) {
                    $('<li>')
                        .attr('title', prompt)
                        .text(prompt)
                        .on('click', function () {
                            $input.val(prompt).trigger('input');
                            $input.focus();
                            closeSamplesDropdown();
                        })
                        .appendTo($list);
                });
            }
            positionSamplesDropdown();
            openSamplesDropdown();
        });
    });

    // Close samples dropdown on any outside click
    $(document).on('click', function (e: JQuery.ClickEvent) {
        if (!$samplesWrap.is(e.target) && $samplesWrap.has(e.target as Element).length === 0) {
            closeSamplesDropdown();
        }
    });

    // ── Helper: refresh the history dropdown lists ──────────────
    function refreshHistoryLists(): void {
        _gateway.GetAIAgentChats(function (data: any) {
            var toMenuItems = function (list: any[]): MenuItem[] {
                return (list || []).map(function (s: any): MenuItem {
                    return { name: s.Name || s.FileName, path: s.FileName };
                });
            };
            window.aiPanel.setRecents(toMenuItems(data.recents));
            window.aiPanel.setFavorites(toMenuItems(data.favorites));
        });
    }

    // ── Helper: build a list of <li> items ──────────────────────
    function buildList($ul: JQuery, items: MenuItem[], emptyMsg: string, isFavorite: boolean): void {
        $ul.empty();
        if (!items || items.length === 0) {
            $ul.append('<li class="ai-panel-dropdown-empty">' + emptyMsg + '</li>');
            return;
        }
        $.each(items, function (_, item: MenuItem) {
            var $li = $('<li>').attr('title', item.name);

            // Label span (clicking it loads the chat)
            var $label = $('<span>').addClass('ai-chat-item-label').text(item.name);
            $label.on('click', function () {
                closeDropdown();
                if (!item.path) return;
                _gateway.LoadAIAgentChat(item.path, isFavorite, function (session: any) {
                    clearConversation();
                    _panelChatFileName = item.path as string;
                    setFavorite(isFavorite);
                    if (session && session.Messages) {
                        $.each(session.Messages, function (_: any, msg: any) {
                            if (msg.Type === 'UserChatMessage' && msg.Content) {
                                $messages.append(makeUserBubble(msg.Content as string));
                            } else if (msg.Type === 'AssistantChatMessage' && msg.Content) {
                                const { cleaned, $actions } = parseReportActions(msg.Content as string);
                                if (cleaned) {
                                    $messages.append(makeAIBubble(formatResponse(cleaned), cleaned));
                                }
                                if ($actions.children().length > 0) {
                                    $messages.append($actions);
                                }
                            }
                        });
                        $messages[0].scrollTop = $messages[0].scrollHeight;
                    }
                });
            });

            // Rename button
            var $renameBtn = $('<button>').addClass('ai-chat-item-btn').attr('title', SWIUtil.tr2('Rename'))
                .html('<i class="fa fa-pencil"></i>');
            $renameBtn.on('click', function (e: JQuery.ClickEvent) {
                e.stopPropagation();
                // Replace label with an inline input
                var $input = $('<input>').addClass('ai-chat-item-rename-input')
                    .val(item.name).attr('maxlength', 60);
                $label.replaceWith($input);
                $input.focus().select();

                function commitRename(): void {
                    var newName = ($input.val() as string).trim();
                    if (!newName || newName === item.name) {
                        $input.replaceWith($label);
                        return;
                    }
                    _gateway.RenameAIAgentChat(item.path as string, newName, isFavorite, function (data: any) {
                        if (data && data.fileName) {
                            item.path = data.fileName;
                            item.name = data.name || newName;
                            $label.text(item.name);
                            $li.attr('title', item.name);
                        }
                        $input.replaceWith($label);
                        // If the renamed chat is the currently open one, update the tracked filename
                        if (_panelChatFileName === (item.path as string) || data && _panelChatFileName === data.fileName) {
                            _panelChatFileName = data.fileName;
                        }
                    });
                }

                $input.on('blur', commitRename);
                $input.on('keydown', function (ev: JQuery.KeyDownEvent) {
                    if (ev.key === 'Enter') { ev.preventDefault(); commitRename(); }
                    if (ev.key === 'Escape') { $input.replaceWith($label); }
                });
            });

            // Delete button
            var $deleteBtn = $('<button>').addClass('ai-chat-item-btn ai-chat-item-btn-delete').attr('title', SWIUtil.tr2('Delete'))
                .html('<i class="fa fa-trash"></i>');
            $deleteBtn.on('click', function (e: JQuery.ClickEvent) {
                e.stopPropagation();
                _gateway.DeleteAIAgentChat(item.path as string, isFavorite, function () {
                    $li.remove();
                    // If deleting the currently open chat, reset the panel state
                    if (_panelChatFileName === item.path) {
                        _panelChatFileName = '';
                        setFavorite(false);
                    }
                    // Show empty message if the list is now empty
                    if ($ul.children().length === 0) {
                        $ul.append('<li class="ai-panel-dropdown-empty">' + emptyMsg + '</li>');
                    }
                });
            });

            // Favorite toggle button
            var $favItemBtn = $('<button>').addClass('ai-chat-item-btn ai-chat-item-btn-fav')
                .attr('title', isFavorite ? SWIUtil.tr2('Remove from favorites') : SWIUtil.tr2('Mark as favorite'))
                .html(isFavorite ? '<i class="fas fa-star"></i>' : '<i class="far fa-star"></i>');
            if (isFavorite) $favItemBtn.addClass('fav-active');
            $favItemBtn.on('click', function (e: JQuery.ClickEvent) {
                e.stopPropagation();
                _gateway.MarkAIAgentChatFavorite(item.path as string, function () {
                    refreshHistoryLists();
                });
            });

            // Action buttons container (hidden until hover via CSS)
            var $actions = $('<span>').addClass('ai-chat-item-actions')
                .append($favItemBtn).append($renameBtn).append($deleteBtn);

            $li.append($label).append($actions).appendTo($ul);
        });
    }

    // ── Public helpers ──────────────────────────────────────────
    window.aiPanel = {
        setFavorite: setFavorite,
        clearConversation: clearConversation,

        // Close panel, clear conversation, reset filename and favorite star.
        // Call this on logout so the panel is fully reset for the next user.
        reset: function (): void {
            // Abort the in-flight request state: stop the progress polling and running
            // animation, re-enable the input (the session is already cleared server-side,
            // so no server cancel call here).
            stopProgressPolling();
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _thinkingSteps = [];
            _requesting = false;
            setSendMode();
            closePanel();
            clearConversation();
            _panelChatFileName = '';
            setFavorite(false);
            // Force a refetch on next open: the next login may be a different user
            // with different agent rights.
            _agentsLoaded = false;
        },

        // Populate the two lists from menu data objects: [{name, path, ...}]
        setFavorites: function (items: MenuItem[]): void {
            buildList($('#ai-panel-fav-list'), items, SWIUtil.tr('No favorites yet'), true);
        },

        setRecents: function (items: MenuItem[]): void {
            buildList($('#ai-panel-mru-list'), items, SWIUtil.tr('No recent items'), false);
        }
    };
}(jQuery));
