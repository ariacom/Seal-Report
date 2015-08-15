var datePattern = '^\\d{1,2}' + escapeSep(dateSeparator) + '\\d{1,2}' + escapeSep(dateSeparator) + '(19|20|21)\\d{2}(\\s([0-1]\\d|[2][0-3])(\\:[0-5]\\d){1,2})?$';
var numericPattern = '^[+-]?([0-9]*' + escapeSep(decimalSeparator) + '?[0-9]+|[0-9]+\.?[0-9]*)([eE][+-]?[0-9]+)?$';
var numericAllowedPattern = '[%$\u20AC£\', ]';

jQuery.fn.dataTableExt.aTypes.unshift(
	function (sData) {
		//Date
		sData = sData.replace(/<.*?>/g, '');
		if (sData == null || sData == "") return null;
		if (sData != null && sData.match(new RegExp(datePattern, 'g'))) return "date-time";
		if (sData == "&nbsp;") return "formatted-num";
	    //Numeric formatted
		
		var strNumeric = convertNumeric(sData);
		if (!strNumeric.match(new RegExp(numericPattern, 'g'))) return null;
		if (strNumeric.match(new RegExp('[0-9][+-]', 'g'))) return null;
		return "formatted-num";
	}
);

jQuery.fn.dataTableExt.oSort['date-time-asc'] = function (a, b) {
	x = calculate_date(a);
	y = calculate_date(b);
	return ((x < y) ? -1 : ((x > y) ?  1 : 0));
};


jQuery.fn.dataTableExt.oSort['date-time-desc'] = function (a, b) {
    x = calculate_date(a);
    y = calculate_date(b);
    return ((x < y) ? 1 : ((x > y) ? -1 : 0));
};	
		
jQuery.fn.dataTableExt.oSort['formatted-num-asc'] = function(x,y){
    return convertNumeric(x)/1 - convertNumeric(y)/1;
}
jQuery.fn.dataTableExt.oSort['formatted-num-desc'] = function(x,y){
    return convertNumeric(y)/1 - convertNumeric(x)/1;
}	

function escapeSep(separator)
{
	if (separator.match(/[\.\'\"\/]/)) return "\\" + separator;
	return separator;
}

function convertNumeric(numValue)
{
  numValue = numValue.replace(new RegExp('&nbsp;', 'g'), '');
  numValue = numValue.replace(/<.*?>/g, '');	
  numValue = numValue.replace(decimalSeparator, '.'); 	
  if (numValue == '') numValue=-2147483648;
  else numValue = numValue.replace(new RegExp(numericAllowedPattern,'g'), '');
  return numValue;
}

function normalizeDateTimeNumber(number) {
    if (number == null) return 0;
    if (number.length == 1) number = 0 + number;
    return number;
}

function calculate_date(date) {
    date = date.replace(/<.*?>/g, '');
	var dateParts = date.split(' ');
	var dayindex = 0, monthindex = 1;
	if (isUSdate) dayindex = 1, monthindex = 0;

	var shortDate = dateParts[0].split(dateSeparator);
	
	var year = 0;
	if (shortDate[2]) year = shortDate[2];
	var month = normalizeDateTimeNumber(shortDate[monthindex]);
	var day = normalizeDateTimeNumber(shortDate[dayindex]);
	
    var result = (year + month + day);
    if (dateParts[1]) {
        timeParts = dateParts[1].split(':');
        var hour = normalizeDateTimeNumber(timeParts[0]);
        var min = 0
        if (timeParts[1]) min = normalizeDateTimeNumber(timeParts[1]);
        var sec = 0
        if (timeParts[2]) sec = normalizeDateTimeNumber(timeParts[2]);
        result = (year + month + day + hour + min + sec);
    }
	return result * 1;
}
