var SWIUtil;
(function (SWIUtil) {
    function tr(reference) {
        var result = translations[reference];
        if (!result || result == "")
            result = reference;
        return result;
    }
    SWIUtil.tr = tr;
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
        $waitDialog.modal('hide');
        SWIUtil.HideMessages();
        var $alert = $("<div class='alert' style='position:absolute; width:100%;z-index: 2000;margin-bottom:0;'><a href='#' class='close' data-dismiss='alert' aria-label='close'>&times;</a><p>" + message + "</p></div>");
        $alert.css("top", ($(window).height() - 54).toString() + "px");
        $alert.addClass(alertClass);
        $("body").append($alert);
        if (timeout == 0)
            timeout = 15000;
        if (timeout > 0)
            setTimeout(function () { $alert.alert('close'); }, timeout);
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
})(SWIUtil || (SWIUtil = {}));
//# sourceMappingURL=swi-utils.js.map