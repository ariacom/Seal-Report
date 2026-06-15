"use strict";
/// <reference path="typings/jquery/JQuery.d.ts" />
/// <reference path="typings/main.d.ts" />
// AI panel
(function ($) {
    var $panel = $('#ai-chat-panel');
    var $toggle = $('#ai-panel-toggle');
    var $close = $('#ai-panel-close');
    var $newBtn = $('#ai-panel-new');
    var $input = $('#ai-panel-input');
    var $send = $('#ai-panel-send');
    var $messages = $('#ai-panel-messages');
    var $favBtn = $('#ai-panel-fav-btn');
    var $histBtn = $('#ai-panel-history-btn');
    var $histDrop = $('#ai-panel-history-dropdown');
    var $histWrap = $histBtn.parent(); // .ai-panel-dropdown-wrap
    var $samplesBtn = $('#ai-panel-samples-btn');
    var $samplesDrop = $('#ai-panel-samples-dropdown');
    var $samplesWrap = $samplesBtn.parent(); // .ai-panel-samples-wrap
    var $agentSelect = $('#ai-panel-agent-select');
    // Move dropdown to <body> so position:fixed is relative to the viewport,
    // not the transformed #ai-chat-panel (transform creates a new stacking context
    // that traps fixed-position descendants).
    $samplesDrop.appendTo('body');
    // ── File name helper ────────────────────────────────────────
    function sanitizeFileName(s) {
        return s.replace(/[\\/:*?"<>|]/g, ' ').replace(/\s+/g, ' ').trim().substring(0, 60) || 'chat';
    }
    // ── Response formatter ──────────────────────────────────────
    function formatResponse(text) {
        if (!text)
            return '';
        // Escape HTML to prevent XSS
        text = text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
        // Inline formatting applied within a single (already escaped) line.
        function inline(s) {
            // `inline code`
            s = s.replace(/`([^`\r\n]+)`/g, '<code>$1</code>');
            // **bold**
            s = s.replace(/\*\*([^*\r\n]+)\*\*/g, '<strong>$1</strong>');
            return s;
        }
        const lines = text.split(/\r\n|\r|\n/);
        let html = '';
        let inList = false;
        const closeList = function () { if (inList) {
            html += '</ul>';
            inList = false;
        } };
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
                if (!inList) {
                    html += '<ul class="ai-list">';
                    inList = true;
                }
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
    function parseReportActions(text) {
        const $actions = $('<div>').addClass('ai-panel-report-actions');
        const re = /\[EXECUTE_REPORT:([^\]\|]+)\|([^\]\|]+)(?:\|([^\]]*))?\]/g;
        const cleaned = text.replace(re, function (_match, rawPath, name, outputGUID) {
            let swiPath;
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
    function fallbackCopy(text) {
        var ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.top = '-1000px';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        try {
            document.execCommand('copy');
        }
        catch (e) { /* ignore */ }
        document.body.removeChild(ta);
    }
    function copyToClipboard(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).catch(function () { fallbackCopy(text); });
        }
        else {
            fallbackCopy(text);
        }
    }
    // ── Markdown → plain text (for copying AI replies) ──────────
    function toPlainText(raw) {
        return (raw || '')
            .replace(/^#{1,6}\s+/gm, '') // strip heading markers
            .replace(/\*\*([^*\r\n]+)\*\*/g, '$1') // strip bold markers
            .replace(/`([^`\r\n]+)`/g, '$1'); // strip inline-code markers
    }
    // ── Bubble factories with hover action toolbars ─────────────
    function bubbleActionBtn(iconClass, title, handler) {
        return $('<button>')
            .addClass('ai-bubble-action-btn')
            .attr('title', title)
            .html('<i class="fa ' + iconClass + '"></i>')
            .on('click', function (e) {
            e.stopPropagation();
            handler();
        });
    }
    function makeUserBubble(text) {
        var $bubble = $('<div>').addClass('ai-panel-bubble user').text(text);
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
            if (!$send.prop('disabled'))
                $send.trigger('click');
        }).appendTo($actions);
        // Rewind: drop this user turn and everything after it, then refill the input
        // so the message can be edited and re-sent.
        bubbleActionBtn('fa-undo', SWIUtil.tr('Rewind to here'), function () {
            rewindToBubble($bubble, text);
        }).appendTo($actions);
        return $bubble.append($actions);
    }
    function makeAIBubble(html, rawText) {
        var $bubble = $('<div>').addClass('ai-panel-bubble ai').html(html);
        var $actions = $('<div>').addClass('ai-bubble-actions');
        bubbleActionBtn('fa-copy', SWIUtil.tr('Copy'), function () {
            copyToClipboard(toPlainText(rawText));
        }).appendTo($actions);
        return $bubble.append($actions);
    }
    // ── Rewind conversation to a user bubble ────────────────────
    // Removes the selected user turn and everything after it (both in the UI and in the
    // server-side agent history), then puts the message text back into the input box so it
    // can be edited and re-sent. The bubble's position among the user bubbles maps 1:1 to
    // the agent's UserChatMessage list, so we send that 0-based index to the server.
    function rewindToBubble($bubble, text) {
        if (_requesting)
            return;
        var index = $bubble.prevAll('.ai-panel-bubble.user').length;
        _gateway.RewindAIAgent(index, function () {
            $bubble.nextAll().remove();
            $bubble.remove();
            $input.val(text).trigger('input');
            $input.focus();
            // Keep the saved chat in sync with the truncated conversation.
            if (_panelChatFileName) {
                _gateway.SaveAIAgentChat(_panelChatFileName, _panelChatInfos, function (saved) {
                    if (saved && saved.fileName)
                        _panelChatFileName = saved.fileName;
                }, undefined, false);
            }
        });
    }
    // ── Clear conversation (UI only) ────────────────────────────
    function clearConversation() {
        $messages.empty();
        $input.val('').trigger('input');
    }
    // ── Panel open / close ──────────────────────────────────────
    function openPanel() {
        $panel.addClass('ai-panel-open');
        $toggle.addClass('ai-panel-open').attr('title', SWIUtil.tr('Hide AI Agent'));
        $('body').addClass('ai-panel-visible');
    }
    function closePanel() {
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
    function applyPanelWidth(px) {
        var clamped = Math.max(MIN_PANEL_WIDTH, Math.min(MAX_PANEL_WIDTH, px));
        document.documentElement.style.setProperty('--ai-panel-width', clamped + 'px');
    }
    // Restore the saved width on load.
    try {
        var saved = window.localStorage.getItem('ai-panel-width');
        if (saved)
            applyPanelWidth(parseInt(saved, 10));
    }
    catch (e) { /* localStorage unavailable */ }
    $resizer.on('mousedown', function (e) {
        e.preventDefault();
        $('body').addClass('ai-resizing');
        function onMove(ev) {
            applyPanelWidth(ev.clientX);
        }
        function onUp() {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            $('body').removeClass('ai-resizing');
            var current = getComputedStyle(document.documentElement)
                .getPropertyValue('--ai-panel-width').trim();
            try {
                window.localStorage.setItem('ai-panel-width', String(parseInt(current, 10)));
            }
            catch (err) { /* localStorage unavailable */ }
        }
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    });
    // ── Agent selector ──────────────────────────────────────
    // Fetches the agents available to the current user. Shows the selectpicker
    // in the header always (≥ 1 agent). When only one is available the control
    // is disabled so it acts as a plain title label.
    var _agentsLoaded = false;
    function loadAgents() {
        if (_agentsLoaded)
            return;
        _agentsLoaded = true;
        _gateway.GetUserAgents(function (data) {
            var agents = data.agents || [];
            if (agents.length === 0)
                return;
            // Rebuild the options.
            $agentSelect.selectpicker('destroy');
            $agentSelect.empty();
            $.each(agents, function (_, a) {
                $('<option>').val(a.guid).text(a.name).appendTo($agentSelect);
            });
            // Disable when there is only one choice — serves as a read-only title.
            $agentSelect.prop('disabled', agents.length === 1);
            // Initialise / refresh the selectpicker.
            $agentSelect.selectpicker({ width: 'auto' });
            if (data.selectedGuid) {
                $agentSelect.val(data.selectedGuid);
                $agentSelect.selectpicker('refresh');
            }
            // Apply Bootstrap tooltips to the rendered dropdown items.
            var $dropdownItems = $agentSelect.closest('.bootstrap-select').find('.dropdown-menu li');
            $.each(agents, function (i, a) {
                if (a.description) {
                    $dropdownItems.eq(i).tooltip({ title: a.description, placement: 'right', container: 'body', trigger: 'hover' });
                }
            });
        });
    }
    $agentSelect.on('change', function () {
        var guid = $agentSelect.val();
        if (!guid)
            return;
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
        if ($panel.hasClass('ai-panel-open'))
            loadAgents();
    });
    // ── Chat persistence state ──────────────────────────────────
    var _panelChatFileName = '';
    var _panelChatInfos = [{ Key: 'Type', Value: 'ai-panel' }];
    // ── New conversation button ─────────────────────────────────
    $newBtn.on('click', function () {
        _gateway.ClearAIAgent(function () {
            clearConversation();
            _panelChatFileName = '';
            setFavorite(false);
        });
    });
    // ── Send button ─────────────────────────────────────────────
    var _requesting = false;
    var $currentTyping = null;
    var $running = $('#ai-panel-running');
    var _runningInterval = null;
    var _runningDots = ['', '.', '..', '...'];
    var _runningDotIndex = 0;
    function setSendMode() {
        $send.find('.send-icon').html('&#10148;');
        $send.removeClass('ai-panel-cancel-btn');
        $input.prop('disabled', false);
        $send.prop('disabled', $input.val().trim() === '');
        $send.attr('title', '');
        if (_runningInterval !== null) {
            window.clearInterval(_runningInterval);
            _runningInterval = null;
        }
        $running.hide();
    }
    function setCancelMode() {
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
        if (!_requesting)
            $send.prop('disabled', $input.val().trim() === '');
    });
    $input.on('keydown', function (e) {
        if (e.key === 'Enter' && !e.altKey) {
            e.preventDefault(); // suppress default newline
            if (!$send.prop('disabled'))
                $send.trigger('click');
        }
        // Alt+Enter → insert a newline at the cursor position
        if (e.key === 'Enter' && e.altKey) {
            e.preventDefault();
            const el = $input[0];
            const start = el.selectionStart;
            const end = el.selectionEnd;
            const val = el.value;
            el.value = val.substring(0, start) + '\n' + val.substring(end);
            el.selectionStart = el.selectionEnd = start + 1;
            $input.trigger('input'); // re-evaluate send-button state
        }
    });
    $send.on('click', function () {
        // ── Cancel mode: interrupt the in-flight request ──────────
        if (_requesting) {
            _gateway.CancelAIAgentResponse(null, null);
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            _requesting = false;
            setSendMode();
            const $cancelled = $('<div>').addClass('ai-panel-bubble ai cancelled').text(SWIUtil.tr('Request cancelled.'));
            $messages.append($cancelled);
            $messages[0].scrollTop = $messages[0].scrollHeight;
            return;
        }
        const message = $input.val().trim();
        if (!message)
            return;
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
        _gateway.GetAIAgentResponse(message, function (data) {
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            if (data.response) {
                const aiBubble = makeAIBubble(formatResponse(data.response || ''), data.response || '');
                $messages.append(aiBubble);
            }
            // Render Execute buttons when the AI proposes running one or more reports
            if (data.reportActions && data.reportActions.length > 0) {
                const $actions = $('<div>').addClass('ai-panel-report-actions');
                data.reportActions.forEach(function (action) {
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
            // Auto-save to Recents after every exchange.
            // On the first save, ask the AI to generate a friendly summary name
            // (the sanitized user message is sent as a fallback).
            var isFirstSave = !_panelChatFileName;
            var saveName = _panelChatFileName || sanitizeFileName(message);
            _gateway.SaveAIAgentChat(saveName, _panelChatInfos, function (saved) {
                if (saved && saved.fileName)
                    _panelChatFileName = saved.fileName;
            }, undefined, isFirstSave);
        }, function (_err) {
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            _requesting = false;
            setSendMode();
            $input.focus();
        });
    });
    // ── Favorite star toggle ────────────────────────────────────
    var _isFavorite = false;
    function setFavorite(state) {
        _isFavorite = state;
        $favBtn.find('i').toggleClass('fa-star', state).toggleClass('fa-star-o', !state);
        $favBtn.toggleClass('fav-active', state);
        $favBtn.attr('title', state ? SWIUtil.tr('Remove from favorites') : SWIUtil.tr('Mark as favorite'));
    }
    $favBtn.on('click', function () {
        if (!_panelChatFileName)
            return;
        var newState = !_isFavorite;
        setFavorite(newState);
        _gateway.MarkAIAgentChatFavorite(_panelChatFileName, function (data) {
            if (data && typeof data.isFavorite !== 'undefined')
                setFavorite(data.isFavorite);
        });
    });
    // ── Favorites / MRU dropdown ────────────────────────────────
    function openDropdown() { $histDrop.show(); }
    function closeDropdown() { $histDrop.hide(); }
    function toggleDropdown() { $histDrop.toggle(); }
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
    $(document).on('click', function (e) {
        if (!$histWrap.is(e.target) && $histWrap.has(e.target).length === 0) {
            closeDropdown();
        }
    });
    // ── Sample prompts button ───────────────────────────────────
    function openSamplesDropdown() { $samplesDrop.show(); }
    function closeSamplesDropdown() { $samplesDrop.hide(); }
    function positionSamplesDropdown() {
        var rect = $samplesBtn[0].getBoundingClientRect();
        var dropW = 600;
        // Anchor the dropdown's bottom just above the button; it grows upward
        // (avoids a gap when the content is shorter than the max-height).
        var bottom = window.innerHeight - rect.top + 6;
        // Open to the right; clamp left so it stays inside the viewport
        var left = rect.right + 6;
        if (left + dropW > window.innerWidth - 8)
            left = window.innerWidth - dropW - 8;
        $samplesDrop.css({ bottom: bottom + 'px', left: left + 'px', right: 'auto', top: 'auto' });
    }
    $samplesBtn.on('click', function () {
        if ($samplesDrop.is(':visible')) {
            closeSamplesDropdown();
            return;
        }
        _gateway.GetAIAgentSamplePrompts(function (data) {
            var $list = $('#ai-panel-samples-list');
            $list.empty();
            var prompts = data.prompts || [];
            if (prompts.length === 0) {
                $list.append('<li class="ai-panel-dropdown-empty">' + SWIUtil.tr('No sample prompts defined') + '</li>');
            }
            else {
                $.each(prompts, function (_, prompt) {
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
    $(document).on('click', function (e) {
        if (!$samplesWrap.is(e.target) && $samplesWrap.has(e.target).length === 0) {
            closeSamplesDropdown();
        }
    });
    // ── Helper: refresh the history dropdown lists ──────────────
    function refreshHistoryLists() {
        _gateway.GetAIAgentChats(function (data) {
            var toMenuItems = function (list) {
                return (list || []).map(function (s) {
                    return { name: s.Name || s.FileName, path: s.FileName };
                });
            };
            window.aiPanel.setRecents(toMenuItems(data.recents));
            window.aiPanel.setFavorites(toMenuItems(data.favorites));
        });
    }
    // ── Helper: build a list of <li> items ──────────────────────
    function buildList($ul, items, emptyMsg, isFavorite) {
        $ul.empty();
        if (!items || items.length === 0) {
            $ul.append('<li class="ai-panel-dropdown-empty">' + emptyMsg + '</li>');
            return;
        }
        $.each(items, function (_, item) {
            var $li = $('<li>').attr('title', item.name);
            // Label span (clicking it loads the chat)
            var $label = $('<span>').addClass('ai-chat-item-label').text(item.name);
            $label.on('click', function () {
                closeDropdown();
                if (!item.path)
                    return;
                _gateway.LoadAIAgentChat(item.path, isFavorite, function (session) {
                    clearConversation();
                    _panelChatFileName = item.path;
                    setFavorite(isFavorite);
                    if (session && session.Messages) {
                        $.each(session.Messages, function (_, msg) {
                            if (msg.Type === 'UserChatMessage' && msg.Content) {
                                $messages.append(makeUserBubble(msg.Content));
                            }
                            else if (msg.Type === 'AssistantChatMessage' && msg.Content) {
                                const { cleaned, $actions } = parseReportActions(msg.Content);
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
            $renameBtn.on('click', function (e) {
                e.stopPropagation();
                // Replace label with an inline input
                var $input = $('<input>').addClass('ai-chat-item-rename-input')
                    .val(item.name).attr('maxlength', 60);
                $label.replaceWith($input);
                $input.focus().select();
                function commitRename() {
                    var newName = $input.val().trim();
                    if (!newName || newName === item.name) {
                        $input.replaceWith($label);
                        return;
                    }
                    _gateway.RenameAIAgentChat(item.path, newName, isFavorite, function (data) {
                        if (data && data.fileName) {
                            item.path = data.fileName;
                            item.name = data.name || newName;
                            $label.text(item.name);
                            $li.attr('title', item.name);
                        }
                        $input.replaceWith($label);
                        // If the renamed chat is the currently open one, update the tracked filename
                        if (_panelChatFileName === item.path || data && _panelChatFileName === data.fileName) {
                            _panelChatFileName = data.fileName;
                        }
                    });
                }
                $input.on('blur', commitRename);
                $input.on('keydown', function (ev) {
                    if (ev.key === 'Enter') {
                        ev.preventDefault();
                        commitRename();
                    }
                    if (ev.key === 'Escape') {
                        $input.replaceWith($label);
                    }
                });
            });
            // Delete button
            var $deleteBtn = $('<button>').addClass('ai-chat-item-btn ai-chat-item-btn-delete').attr('title', SWIUtil.tr('Delete'))
                .html('<i class="fa fa-trash"></i>');
            $deleteBtn.on('click', function (e) {
                e.stopPropagation();
                _gateway.DeleteAIAgentChat(item.path, isFavorite, function () {
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
            if (isFavorite)
                $favItemBtn.addClass('fav-active');
            $favItemBtn.on('click', function (e) {
                e.stopPropagation();
                _gateway.MarkAIAgentChatFavorite(item.path, function () {
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
        reset: function () {
            closePanel();
            clearConversation();
            _panelChatFileName = '';
            setFavorite(false);
        },
        // Populate the two lists from menu data objects: [{name, path, ...}]
        setFavorites: function (items) {
            buildList($('#ai-panel-fav-list'), items, SWIUtil.tr('No favorites yet'), true);
        },
        setRecents: function (items) {
            buildList($('#ai-panel-mru-list'), items, SWIUtil.tr('No recent items'), false);
        }
    };
}(jQuery));
//# sourceMappingURL=swi-ai.js.map