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
    // ── New conversation button ─────────────────────────────────
    $newBtn.on('click', function () {
        _gateway.ClearAIAssistant(function () {
            clearConversation();
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
            const aiBubble = $('<div>').addClass('ai-panel-bubble ai').html(formatResponse(data.response || ''));
            $messages.append(aiBubble);
            $messages[0].scrollTop = $messages[0].scrollHeight;
            _requesting = false;
            setSendMode();
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
        var $icon = $favBtn.find('i');
        if (state) {
            $icon.attr('class', 'fa fa-star');
            $favBtn.addClass('fav-active').attr('title', 'Remove from favorites');
        }
        else {
            $icon.attr('class', 'fa fa-star-o');
            $favBtn.removeClass('fav-active').attr('title', 'Mark as favorite');
        }
    }
    $favBtn.on('click', function () {
        setFavorite(!_isFavorite);
        // API call will be wired here later
    });
    // ── Favorites / MRU dropdown ────────────────────────────────
    function openDropdown() { $histDrop.show(); }
    function closeDropdown() { $histDrop.hide(); }
    function toggleDropdown() { $histDrop.toggle(); }
    $histBtn.on('click', function () {
        // No stopPropagation — letting the event reach Bootstrap's document handler
        // so navbar dropdowns can still close normally.
        toggleDropdown();
    });
    // Close our dropdown on any outside click.
    // We check the target rather than stopping propagation, so Bootstrap's own
    // document-level listener for navbar dropdowns is never blocked.
    $(document).on('click', function (e) {
        if (!$histWrap.is(e.target) && $histWrap.has(e.target).length === 0) {
            closeDropdown();
        }
    });
    // ── Helper: build a list of <li> items ──────────────────────
    function buildList($ul, items, emptyMsg) {
        $ul.empty();
        if (!items || items.length === 0) {
            $ul.append('<li class="ai-panel-dropdown-empty">' + emptyMsg + '</li>');
            return;
        }
        $.each(items, function (_, item) {
            $('<li>')
                .attr('title', item.name)
                .text(item.name)
                .data({ path: item.path || '', viewGuid: item.viewGUID || '', outputGuid: item.outputGUID || '' })
                .on('click', function () {
                closeDropdown();
                // Navigation will be wired here later
            })
                .appendTo($ul);
        });
    }
    // ── Public helpers ──────────────────────────────────────────
    window.aiPanel = {
        setFavorite: setFavorite,
        clearConversation: clearConversation,
        // Populate the two lists from menu data objects: [{name, path, ...}]
        setFavorites: function (items) {
            buildList($('#ai-panel-fav-list'), items, 'No favorites yet');
        },
        setRecents: function (items) {
            buildList($('#ai-panel-mru-list'), items, 'No recent items');
        }
    };
}(jQuery));
//# sourceMappingURL=swi-ai.js.map