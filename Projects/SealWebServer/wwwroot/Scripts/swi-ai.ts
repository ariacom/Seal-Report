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
    function formatResponse(text: string): string {
        if (!text) return '';
        // Escape HTML to prevent XSS
        text = text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');

        // Inline formatting applied within a single (already escaped) line.
        function inline(s: string): string {
            // `inline code`
            s = s.replace(/`([^`\r\n]+)`/g, '<code>$1</code>');
            // **bold**
            s = s.replace(/\*\*([^*\r\n]+)\*\*/g, '<strong>$1</strong>');
            return s;
        }

        const lines = text.split(/\r\n|\r|\n/);
        let html = '';
        let inList = false;
        const closeList = function () { if (inList) { html += '</ul>'; inList = false; } };

        for (const line of lines) {
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
            $('<button>')
                .addClass('ai-panel-execute-btn')
                .html('<i class="fa fa-play"></i> ' + $('<span>').text(name.trim()).html())
                .on('click', function () { _gateway.ExecuteReport(swiPath, '', outputGUID || ''); })
                .appendTo($actions);
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
        bubbleActionBtn('fa-copy', SWIUtil.tr('Copy'), function () {
            copyToClipboard(text);
        }).appendTo($actions);
        bubbleActionBtn('fa-clipboard', SWIUtil.tr('Copy and paste'), function () {
            copyToClipboard(text);
            $input.val(text).trigger('input');
            $input.focus();
        }).appendTo($actions);
        bubbleActionBtn('fa-paper-plane', SWIUtil.tr('Copy, paste and send'), function () {
            copyToClipboard(text);
            $input.val(text).trigger('input');
            if (!$send.prop('disabled')) $send.trigger('click');
        }).appendTo($actions);
        return $bubble.append($actions);
    }

    function makeAIBubble(html: string, rawText: string): JQuery {
        var $bubble  = $('<div>').addClass('ai-panel-bubble ai').html(html);
        var $actions = $('<div>').addClass('ai-bubble-actions');
        bubbleActionBtn('fa-copy', SWIUtil.tr('Copy'), function () {
            copyToClipboard(toPlainText(rawText));
        }).appendTo($actions);
        return $bubble.append($actions);
    }

    // ── Clear conversation (UI only) ────────────────────────────
    function clearConversation(): void {
        $messages.empty();
        $input.val('').trigger('input');
    }

    // ── Panel open / close ──────────────────────────────────────
    function openPanel(): void {
        $panel.addClass('ai-panel-open');
        $toggle.addClass('ai-panel-open').attr('title', SWIUtil.tr('Hide AI Agent'));
        $('body').addClass('ai-panel-visible');
    }

    function closePanel(): void {
        $panel.removeClass('ai-panel-open');
        $toggle.removeClass('ai-panel-open').attr('title', SWIUtil.tr('AI Agent'));
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

            // Initialise / refresh the selectpicker.
            ($agentSelect as any).selectpicker({ width: 'auto' });
            if (data.selectedGuid) {
                $agentSelect.val(data.selectedGuid);
                ($agentSelect as any).selectpicker('refresh');
            }

            // Apply Bootstrap tooltips to the rendered dropdown items.
            var $dropdownItems = $agentSelect.closest('.bootstrap-select').find('.dropdown-menu li');
            $.each(agents, function (i: number, a: { guid: string; name: string; description: string }) {
                if (a.description) {
                    ($dropdownItems.eq(i) as any).tooltip({ title: a.description, placement: 'right', container: 'body', trigger: 'hover' });
                }
            });
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
    var $currentTyping: JQuery | null = null;
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
        $send.attr('title', SWIUtil.tr('Cancel'));
        _runningDotIndex = 0;
        $running.text(SWIUtil.tr('Running')).show();
        _runningInterval = window.setInterval(function () {
            _runningDotIndex = (_runningDotIndex + 1) % _runningDots.length;
            $running.text(SWIUtil.tr('Running') + _runningDots[_runningDotIndex]);
        }, 400);
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
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _requesting = false;
            setSendMode();
            const $cancelled = $('<div>').addClass('ai-panel-bubble ai cancelled').text(SWIUtil.tr('Request cancelled.'));
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

        // Typing indicator — three animated dots
        $currentTyping = $('<div>').addClass('ai-panel-bubble ai typing')
            .html('<span class="dot"></span><span class="dot"></span><span class="dot"></span>');
        $messages.append($currentTyping);
        $messages[0].scrollTop = $messages[0].scrollHeight;

        _requesting = true;
        setCancelMode();

        _gateway.GetAIAgentResponse(message, function (data: any) {
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            if (data.response) {
                const aiBubble = makeAIBubble(formatResponse(data.response || ''), data.response || '');
                $messages.append(aiBubble);
            }
            // Render Execute buttons when the AI proposes running one or more reports
            if (data.reportActions && (data.reportActions as any[]).length > 0) {
                const $actions = $('<div>').addClass('ai-panel-report-actions');
                (data.reportActions as Array<{ path: string; name: string; outputGUID: string }>).forEach(function (action) {
                    $('<button>')
                        .addClass('ai-panel-execute-btn')
                        .html('<i class="fa fa-play"></i> ' + $('<span>').text(action.name).html())
                        .on('click', function () {
                            _gateway.ExecuteReport(action.path, '', action.outputGUID || '');
                        })
                        .appendTo($actions);
                });
                $messages.append($actions);
            }

            $messages[0].scrollTop = $messages[0].scrollHeight;
            _requesting = false;
            setSendMode();
            $input.focus();
            // Auto-save to Recents after every exchange
            // On the first save use the user's message as a friendly name
            var saveName = _panelChatFileName || sanitizeFileName(message);
            _gateway.SaveAIAgentChat(saveName, _panelChatInfos, function (saved: any) {
                if (saved && saved.fileName) _panelChatFileName = saved.fileName;
            });
        }, function (_err: any) {
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
        $favBtn.find('i').toggleClass('fa-star', state).toggleClass('fa-star-o', !state);
        $favBtn.toggleClass('fav-active', state);
        $favBtn.attr('title', state ? SWIUtil.tr('Remove from favorites') : SWIUtil.tr('Mark as favorite'));
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
    function openDropdown():  void { $histDrop.show(); }
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
            var $renameBtn = $('<button>').addClass('ai-chat-item-btn').attr('title', SWIUtil.tr('Rename'))
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
            var $deleteBtn = $('<button>').addClass('ai-chat-item-btn ai-chat-item-btn-delete').attr('title', SWIUtil.tr('Delete'))
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
                .attr('title', isFavorite ? SWIUtil.tr('Remove from favorites') : SWIUtil.tr('Mark as favorite'))
                .html(isFavorite ? '<i class="fa fa-star"></i>' : '<i class="fa fa-star-o"></i>');
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
            closePanel();
            clearConversation();
            _panelChatFileName = '';
            setFavorite(false);
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
