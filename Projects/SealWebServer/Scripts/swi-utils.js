var SWIUtil;
(function (SWIUtil) {
    function tr(reference) {
        var result = tra[reference];
        if (!result || result == "")
            result = reference;
        return result;
    }
    SWIUtil.tr = tr;
    function tr2(reference) {
        return $('<div/>').html(tr(reference)).text();
    }
    SWIUtil.tr2 = tr2;
    function Newguid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
    SWIUtil.Newguid = Newguid;
    function GetReportName(path) {
        return path.split('\\').pop().replace(".srex", "");
    }
    SWIUtil.GetReportName = GetReportName;
    function GetDirectoryName(path) {
        return path.substring(0, path.lastIndexOf("\\"));
    }
    SWIUtil.GetDirectoryName = GetDirectoryName;
    function ShowMessage(alertClass, message, timeout) {
        setTimeout(function () {
            $waitDialog.modal('hide');
            SWIUtil.HideMessages();
            var $alert = $("<div class='alert' style='position:absolute; width:80%; z-index: 2000;margin-bottom:0;'><a href='#' class='close' data-dismiss='alert' aria-label='close'>&times;</a><p>" + message + "</p></div>");
            $alert.css("top", ($(window).height() - 54).toString() + "px");
            $alert.css("left", ($(window).width() / 10).toString() + "px");
            $alert.addClass(alertClass);
            $("body").append($alert);
            if (timeout == 0)
                timeout = 15000;
            if (timeout > 0)
                setTimeout(function () { $alert.alert('close'); }, timeout);
        }, 500);
    }
    SWIUtil.ShowMessage = ShowMessage;
    function HideMessages() {
        $('.alert').alert('close');
    }
    SWIUtil.HideMessages = HideMessages;
    function EnableButton(button, enabled) {
        if (!enabled)
            button.prop('disabled', 'true').addClass("disabled");
        else
            button.removeProp('disabled').removeClass("disabled");
    }
    SWIUtil.EnableButton = EnableButton;
    function EnableLinkInput(link, enabled) {
        if (!enabled)
            link.attr('disabled', 'true');
        else
            link.removeAttr('disabled');
    }
    SWIUtil.EnableLinkInput = EnableLinkInput;
    function IsEnabled(control) {
        return !(control.attr('disabled') || control.prop('disabled'));
    }
    SWIUtil.IsEnabled = IsEnabled;
    function ActivatePanel(button, panel, active) {
        if (!active) {
            button.removeClass("active");
            panel.hide();
        }
        else {
            button.addClass("active");
            panel.show();
        }
    }
    SWIUtil.ActivatePanel = ActivatePanel;
    function ShowHideControl(control, show) {
        if (!show)
            control.hide();
        else
            control.show();
    }
    SWIUtil.ShowHideControl = ShowHideControl;
    function GetOption(val, text, valSelected, icon) {
        var $result = $("<option>").attr("value", val).html(text ? text : val);
        if (icon)
            $result.attr("data-icon", icon);
        if (val == valSelected)
            $result.attr("selected", "true");
        return $result;
    }
    SWIUtil.GetOption = GetOption;
    function GetAnchorWithIcon(text, id, type, icon) {
        var $a = $("<a/>").text(text);
        $a.html("<i class='" + icon + "'></i> " + $a.html());
        if (id)
            $a.prop("id", id);
        if (type)
            $a.prop("type", type);
        return $a;
    }
    SWIUtil.GetAnchorWithIcon = GetAnchorWithIcon;
    function UniqueName(name, array) {
        var result = name;
        var index = 1;
        while (true) {
            var found = false;
            $.each(array, function (key, value) {
                if (value.name == result)
                    found = true;
            });
            if (found)
                result = name + index.toString();
            else
                break;
            index++;
        }
        return result;
    }
    SWIUtil.UniqueName = UniqueName;
    function GetAggregateName(aggr) {
        if (aggr == 1)
            return SWIUtil.tr2("Minimum of");
        else if (aggr == 2)
            return SWIUtil.tr2("Maximum of");
        else if (aggr == 3)
            return SWIUtil.tr2("Average of");
        else if (aggr == 4)
            return SWIUtil.tr2("Count of");
        return "";
    }
    SWIUtil.GetAggregateName = GetAggregateName;
    function FindBootstrapEnvironment() {
        var envs = ['xs', 'sm', 'md', 'lg'];
        var $el = $('<div>');
        $el.appendTo($('body'));
        for (var i = envs.length - 1; i >= 0; i--) {
            var env = envs[i];
            $el.addClass('hidden-' + env);
            if ($el.is(':hidden')) {
                $el.remove();
                return env;
            }
        }
    }
    SWIUtil.FindBootstrapEnvironment = FindBootstrapEnvironment;
    function IsMobile() {
        return SWIUtil.FindBootstrapEnvironment() == "xs";
    }
    SWIUtil.IsMobile = IsMobile;
    function InitNumericInput() {
        $(".numeric_input").keyup(function () {
            var v = this.value;
            if (!$.isNumeric(v)) {
                this.value = this.value.slice(0, -1);
            }
        });
    }
    SWIUtil.InitNumericInput = InitNumericInput;
})(SWIUtil || (SWIUtil = {}));
//# sourceMappingURL=swi-utils.js.map