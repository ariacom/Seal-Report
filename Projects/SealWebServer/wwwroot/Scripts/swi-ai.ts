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
        // **bold**
        text = text.replace(/\*\*([^*\r\n]+)\*\*/g, '<strong>$1</strong>');
        // Line breaks
        text = text.replace(/\r\n|\r|\n/g, '<br>');
        return text;
    }

    // ── Clear conversation (UI only) ────────────────────────────
    function clearConversation(): void {
        $messages.empty();
        $input.val('').trigger('input');
    }

    // ── Panel open / close ──────────────────────────────────────
    function openPanel(): void {
        $panel.addClass('ai-panel-open');
        $toggle.addClass('ai-panel-open').attr('title', 'Hide AI Assistant');
        $('body').addClass('ai-panel-visible');
    }

    function closePanel(): void {
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
    var _panelChatFileName: string = '';
    var _panelChatInfos: { Key: string; Value: string }[] = [{ Key: 'Type', Value: 'ai-panel' }];

    // ── New conversation button ─────────────────────────────────
    $newBtn.on('click', function () {
        _gateway.ClearAIAssistant(function () {
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
            _gateway.CancelAIAssistantResponse(null, null);
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _requesting = false;
            setSendMode();
            const $cancelled = $('<div>').addClass('ai-panel-bubble ai cancelled').text('Request cancelled.');
            $messages.append($cancelled);
            $messages[0].scrollTop = $messages[0].scrollHeight;
            return;
        }

        const message = ($input.val() as string).trim();
        if (!message) return;

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

        _gateway.GetAIAssistantResponse(message, function (data: any) {
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            if (data.response) {
                const aiBubble = $('<div>').addClass('ai-panel-bubble ai').html(formatResponse(data.response || ''));
                $messages.append(aiBubble);
            }
            // Render Execute buttons when the AI proposes running one or more reports
            if (data.reportActions && (data.reportActions as any[]).length > 0) {
                const $actions = $('<div>').addClass('ai-panel-report-actions');
                (data.reportActions as Array<{ path: string; name: string }>).forEach(function (action) {
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
            _gateway.SaveAIAssistantChat(saveName, _panelChatInfos, function (saved: any) {
                if (saved && saved.fileName) _panelChatFileName = saved.fileName;
            });
        }, function (_err: any) {
            if ($currentTyping) { $currentTyping.remove(); $currentTyping = null; }
            _requesting = false;
            setSendMode();
        });
    });

    // ── Favorite star toggle ────────────────────────────────────
    var _isFavorite: boolean = false;

    function setFavorite(state: boolean): void {
        _isFavorite = state;
        var $icon = $favBtn.find('i');
        if (state) {
            $icon.attr('class', 'fa fa-star');
            $favBtn.addClass('fav-active').attr('title', 'Remove from favorites');
        } else {
            $icon.attr('class', 'fa fa-star-o');
            $favBtn.removeClass('fav-active').attr('title', 'Mark as favorite');
        }
    }

    $favBtn.on('click', function () {
        if (!_panelChatFileName) return; // nothing saved yet
        _gateway.MarkAIAssistantChatFavorite(_panelChatFileName, function (data: any) {
            if (data) setFavorite(data.isFavorite);
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
            _gateway.GetAIAssistantChats(function (data: any) {
                var toMenuItems = function (list: any[]): MenuItem[] {
                    return (list || []).map(function (s: any): MenuItem {
                        return { name: s.Name || s.FileName, path: s.FileName };
                    });
                };
                window.aiPanel.setRecents(toMenuItems(data.recents));
                window.aiPanel.setFavorites(toMenuItems(data.favorites));
            });
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
        var top  = rect.top - 600;
        // Open to the right; clamp left so it stays inside the viewport
        var left = rect.right + 6;
        if (left + dropW > window.innerWidth - 8) left = window.innerWidth - dropW - 8;
        $samplesDrop.css({ top: top + 'px', left: left + 'px', right: 'auto', bottom: 'auto' });
    }

    $samplesBtn.on('click', function () {
        if ($samplesDrop.is(':visible')) {
            closeSamplesDropdown();
            return;
        }
        _gateway.GetAIAssistantSamplePrompts(function (data: any) {
            var $list = $('#ai-panel-samples-list');
            $list.empty();
            var prompts: string[] = data.prompts || [];
            if (prompts.length === 0) {
                $list.append('<li class="ai-panel-dropdown-empty">No sample prompts defined</li>');
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

    // ── Helper: build a list of <li> items ──────────────────────
    function buildList($ul: JQuery, items: MenuItem[], emptyMsg: string, isFavorite: boolean): void {
        $ul.empty();
        if (!items || items.length === 0) {
            $ul.append('<li class="ai-panel-dropdown-empty">' + emptyMsg + '</li>');
            return;
        }
        $.each(items, function (_, item: MenuItem) {
            $('<li>')
                .attr('title', item.name)
                .text(item.name)
                .data({ path: item.path || '', viewGuid: item.viewGUID || '', outputGuid: item.outputGUID || '' })
                .on('click', function () {
                    closeDropdown();
                    if (!item.path) return;
                    _gateway.LoadAIAssistantChat(item.path, isFavorite, function (session: any) {
                        clearConversation();
                        _panelChatFileName = item.path as string;
                        setFavorite(isFavorite);
                        // Replay the saved messages in the panel
                        if (session && session.Messages) {
                            $.each(session.Messages, function (_: any, msg: any) {
                                if (msg.Type === 'UserChatMessage' && msg.Content) {
                                    $messages.append($('<div>').addClass('ai-panel-bubble user').text(msg.Content));
                                } else if (msg.Type === 'AssistantChatMessage' && msg.Content) {
                                    $messages.append($('<div>').addClass('ai-panel-bubble ai').html(formatResponse(msg.Content)));
                                }
                            });
                            $messages[0].scrollTop = $messages[0].scrollHeight;
                        }
                    });
                })
                .appendTo($ul);
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
            buildList($('#ai-panel-fav-list'), items, 'No favorites yet', true);
        },

        setRecents: function (items: MenuItem[]): void {
            buildList($('#ai-panel-mru-list'), items, 'No recent items', false);
        }
    };
}(jQuery));
