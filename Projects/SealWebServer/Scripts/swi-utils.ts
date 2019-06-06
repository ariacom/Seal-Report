declare var tra;
declare var availableDateKeywords;
declare var shortDateFormat;
declare var shortDateTimeFormat;
declare var languageName;

module SWIUtil {
    export function tr(reference: string): string {
        var result = tra[reference];
        if (!result || result == "") result = reference;
        return result;
    }

    export function tr2(reference: string): string {
        return $('<div/>').html(tr(reference)).text();
    }

    export function Newguid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        })
    }

    export function GetReportName(path: string): string {
        return path.split('\\').pop().replace(".srex", "");
    }

    export function GetDirectoryName(path: string): string {
        return path.substring(0, path.lastIndexOf("\\"));
    }

    export function ShowMessage(alertClass: string, message: string, timeout: number) {
        setTimeout(function () {
        $waitDialog.modal('hide');
        SWIUtil.HideMessages();
        var $alert = $("<div class='alert' style='position:absolute; width:80%; z-index: 2000;margin-bottom:0;'><a href='#' class='close' data-dismiss='alert' aria-label='close'>&times;</a><p>" + message + "</p></div>");
        $alert.css("top", ($(window).height() - 54).toString() + "px");
        $alert.css("left", ($(window).width()/10).toString() + "px");
        $alert.addClass(alertClass);
        $("body").append($alert);
        if (timeout == 0) timeout = 15000;
        if (timeout > 0) setTimeout(function () { $alert.alert('close'); }, timeout);
        }, 500);
    }

    export function HideMessages() {
        $('.alert').alert('close');
    }

    export function EnableButton(button: JQuery, enabled: boolean) {
        if (!enabled) button.prop('disabled', 'true').addClass("disabled");
        else button.removeProp('disabled').removeClass("disabled");
    }

    export function EnableLinkInput(link: JQuery, enabled: boolean) {
        if (!enabled) link.attr('disabled', 'true');
        else link.removeAttr('disabled');
    }

    export function IsEnabled(control: JQuery): boolean {
        return !(control.attr('disabled') || control.prop('disabled'))
    }

    export function ActivatePanel(button: JQuery, panel: JQuery, active: boolean) {
        if (!active) {
            button.removeClass("active");
            panel.hide();
        }
        else {
            button.addClass("active");
            panel.show();
        }
    }

    export function ShowHideControl(control: JQuery, show: boolean) {
        if (!show) control.hide();
        else control.show();
    }

    export function GetOption(val: string, text: string, valSelected: string, icon?: string) {
        var $result = $("<option>").attr("value", val).html(text ? text : val);
        if (icon) $result.attr("data-icon", icon);
        if (val == valSelected) $result.attr("selected", "true");
        return $result;
    }

    export function GetAnchorWithIcon(text: string, id: string, type: string, icon: string): JQuery {
        var $a = $("<a/>").text(text);
        $a.html("<i class='" + icon + "'></i> " + $a.html());
        if (id) $a.prop("id", id);
        if (type) $a.prop("type", type);
        return $a;
    }

    export function UniqueName(name: string, array: any) {
        var result = name;
        var index = 1;
        while (true) {
            var found = false;
            $.each(array, function (key, value) {
                if (value.name == result) found = true;
            });

            if (found) result = name + index.toString();
            else break;
            index++;
        }
        return result;
    }

    export function GetAggregateName(aggr: number) {
        if (aggr == 1) return SWIUtil.tr2("Minimum of");
        else if (aggr == 2) return SWIUtil.tr2("Maximum of");
        else if (aggr == 3) return SWIUtil.tr2("Average of");
        else if (aggr == 4) return SWIUtil.tr2("Count of");
        return "";
    }

    export function FindBootstrapEnvironment() {
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

    export function IsMobile() {
        return SWIUtil.FindBootstrapEnvironment() == "xs";
    }

    export function InitNumericInput() {
        $(".numeric_input").keyup(function () {
            var v = this.value;
            if (!$.isNumeric(v)) {
                this.value = this.value.slice(0, -1);
            }
        });
    }
}