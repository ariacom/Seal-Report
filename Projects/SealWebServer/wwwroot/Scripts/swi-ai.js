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
    var $histBtn = $('#ai-panel-history-btn');
    var $histDrop = $('#ai-panel-history-dropdown');
    var $histWrap = $histBtn.parent(); // .ai-panel-dropdown-wrap
    var $samplesBtn = $('#ai-panel-samples-btn');
    var $samplesDrop = $('#ai-panel-samples-dropdown');
    var $samplesWrap = $samplesBtn.parent(); // .ai-panel-samples-wrap
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
        // **bold**
        text = text.replace(/\*\*([^*\r\n]+)\*\*/g, '<strong>$1</strong>');
        // Line breaks
        text = text.replace(/\r\n|\r|\n/g, '<br>');
        return text;
    }
    // ── Report-action parser ────────────────────────────────────
    // Strips [EXECUTE_REPORT:path|name] tags from text, returns cleaned text
    // and a jQuery element (possibly empty) containing Execute buttons.
    function parseReportActions(text) {
        const $actions = $('<div>').addClass('ai-panel-report-actions');
        const re = /\[EXECUTE_REPORT:([^\]\|]+)\|([^\]]+)\]/g;
        const cleaned = text.replace(re, function (_match, rawPath, name) {
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
                .on('click', function () { _gateway.ExecuteReport(swiPath, '', ''); })
                .appendTo($actions);
            return '';
        });
        return { cleaned: cleaned.trim(), $actions };
    }
    // ── Clear conversation (UI only) ────────────────────────────
    function clearConversation() {
        $messages.empty();
        $input.val('').trigger('input');
    }
    // ── Panel open / close ──────────────────────────────────────
    function openPanel() {
        $panel.addClass('ai-panel-open');
        $toggle.addClass('ai-panel-open').attr('title', 'Hide AI Assistant');
        $('body').addClass('ai-panel-visible');
    }
    function closePanel() {
        $panel.removeClass('ai-panel-open');
        $toggle.removeClass('ai-panel-open').attr('title', 'AI Assistant');
        $('body').removeClass('ai-panel-visible');
        closeDropdown();
    }
    $toggle.on('click', function () {
        $panel.hasClass('ai-panel-open') ? closePanel() : openPanel();
    });
    $close.on('click', closePanel);
    // ── Chat persistence state ──────────────────────────────────
    var _panelChatFileName = '';
    var _panelChatInfos = [{ Key: 'Type', Value: 'ai-panel' }];
    // ── New conversation button ─────────────────────────────────
    $newBtn.on('click', function () {
        _gateway.ClearAIAssistant(function () {
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
        $send.attr('title', 'Cancel');
        _runningDotIndex = 0;
        $running.text('Running').show();
        _runningInterval = window.setInterval(function () {
            _runningDotIndex = (_runningDotIndex + 1) % _runningDots.length;
            $running.text('Running' + _runningDots[_runningDotIndex]);
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
            _gateway.CancelAIAssistantResponse(null, null);
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            _requesting = false;
            setSendMode();
            const $cancelled = $('<div>').addClass('ai-panel-bubble ai cancelled').text('Request cancelled.');
            $messages.append($cancelled);
            $messages[0].scrollTop = $messages[0].scrollHeight;
            return;
        }
        const message = $input.val().trim();
        if (!message)
            return;
        // Append user bubble immediately
        const userBubble = $('<div>').addClass('ai-panel-bubble user').text(message);
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
        _gateway.GetAIAssistantResponse(message, function (data) {
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            if (data.response) {
                const aiBubble = $('<div>').addClass('ai-panel-bubble ai').html(formatResponse(data.response || ''));
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
                        _gateway.ExecuteReport(action.path, '', '');
                    })
                        .appendTo($actions);
                });
                $messages.append($actions);
            }
            $messages[0].scrollTop = $messages[0].scrollHeight;
            _requesting = false;
            setSendMode();
            // Auto-save to Recents after every exchange
            // On the first save use the user's message as a friendly name
            var saveName = _panelChatFileName || sanitizeFileName(message);
            _gateway.SaveAIAssistantChat(saveName, _panelChatInfos, function (saved) {
                if (saved && saved.fileName)
                    _panelChatFileName = saved.fileName;
            });
        }, function (_err) {
            if ($currentTyping) {
                $currentTyping.remove();
                $currentTyping = null;
            }
            _requesting = false;
            setSendMode();
        });
    });
    // ── Favorite star toggle ────────────────────────────────────
    var _isFavorite = false;
    function setFavorite(state) {
        _isFavorite = state;
    }
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
        var top = rect.top - 600;
        // Open to the right; clamp left so it stays inside the viewport
        var left = rect.right + 6;
        if (left + dropW > window.innerWidth - 8)
            left = window.innerWidth - dropW - 8;
        $samplesDrop.css({ top: top + 'px', left: left + 'px', right: 'auto', bottom: 'auto' });
    }
    $samplesBtn.on('click', function () {
        if ($samplesDrop.is(':visible')) {
            closeSamplesDropdown();
            return;
        }
        _gateway.GetAIAssistantSamplePrompts(function (data) {
            var $list = $('#ai-panel-samples-list');
            $list.empty();
            var prompts = data.prompts || [];
            if (prompts.length === 0) {
                $list.append('<li class="ai-panel-dropdown-empty">No sample prompts defined</li>');
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
        _gateway.GetAIAssistantChats(function (data) {
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
                _gateway.LoadAIAssistantChat(item.path, isFavorite, function (session) {
                    clearConversation();
                    _panelChatFileName = item.path;
                    setFavorite(isFavorite);
                    if (session && session.Messages) {
                        $.each(session.Messages, function (_, msg) {
                            if (msg.Type === 'UserChatMessage' && msg.Content) {
                                $messages.append($('<div>').addClass('ai-panel-bubble user').text(msg.Content));
                            }
                            else if (msg.Type === 'AssistantChatMessage' && msg.Content) {
                                const { cleaned, $actions } = parseReportActions(msg.Content);
                                if (cleaned) {
                                    $messages.append($('<div>').addClass('ai-panel-bubble ai').html(formatResponse(cleaned)));
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
            var $renameBtn = $('<button>').addClass('ai-chat-item-btn').attr('title', 'Rename')
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
                    _gateway.RenameAIAssistantChat(item.path, newName, isFavorite, function (data) {
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
            var $deleteBtn = $('<button>').addClass('ai-chat-item-btn ai-chat-item-btn-delete').attr('title', 'Delete')
                .html('<i class="fa fa-trash"></i>');
            $deleteBtn.on('click', function (e) {
                e.stopPropagation();
                _gateway.DeleteAIAssistantChat(item.path, isFavorite, function () {
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
                .attr('title', isFavorite ? 'Remove from favorites' : 'Mark as favorite')
                .html(isFavorite ? '<i class="fa fa-star"></i>' : '<i class="fa fa-star-o"></i>');
            if (isFavorite)
                $favItemBtn.addClass('fav-active');
            $favItemBtn.on('click', function (e) {
                e.stopPropagation();
                _gateway.MarkAIAssistantChatFavorite(item.path, function () {
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
            buildList($('#ai-panel-fav-list'), items, 'No favorites yet', true);
        },
        setRecents: function (items) {
            buildList($('#ai-panel-mru-list'), items, 'No recent items', false);
        }
    };
}(jQuery));
//# sourceMappingURL=swi-ai.js.map