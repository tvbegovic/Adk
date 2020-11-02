angular.module('app')
.filter('reverse', function () {
    return function (items) {
        return items.slice().reverse();
    };
})
.filter('skip', function () {
    return function (list, skipCount) {
        if (!list)
            return list;
        if (skipCount > 0)
            return list.slice(skipCount);
        else
            return list.slice(0, list.length + skipCount);
    };
})
.filter('range', function () {
    return function (input, total) {
        total = parseInt(total);
        for (var i = 0; i < total; i++) {
            input.push(i);
        }

        return input;
    };
})
.filter('isoDate', function () {
    return function (sIsoDate, format) {
        if (!sIsoDate) {
            return null;
        }

        return moment(sIsoDate).format(format || 'lll');
    };
})
.filter('weekday', function () {
    return function (inputDate) {
        if (!inputDate) {
            return null;
        }

        return moment(inputDate).format('dddd');
    };
})
.filter('userDate', ['$filter',
function ($filter) {

    var angularDateFilter = $filter('date');

    function getUserDateFormat(inputDate) {

        if (!inputDate) {
            return '';
        }

        return angularDateFilter(new Date(inputDate), localStorage.UserDateFormat);
    }

    return getUserDateFormat;
}])
.filter('padZero', function () {
    return function (n, len) {
        var num = parseInt(n, 10);
        len = parseInt(len, 10);
        if (isNaN(num) || isNaN(len)) {
            return n;
        }
        num = ''+num;
        while (num.length < len) {
            num = '0'+num;
        }
        return num;
    };
})
.filter('dashIfNull', function () {
    return function (o) {
        return o ? o : '--';
    };
})
.filter('nullIfZero', function () {
    return function (o) {
        return o === 0 ? null : o;
    };
})
.filter('percentOf', function () {
    return function (f, max) {
        return '' + (f / max * 100) + '%';
    };
})
.filter('asPercent', ['$filter', function ($filter) {
    return function (f, max, precision) {
        if (!max) {
            return '-';
        }

        return '' + $filter('number')(f / max * 100, precision) + '%';
    };
}])
.filter('megabytes', ['$filter', function ($filter) {
    return function (bytes, precision) {
        if (!bytes) {
            return null;
        }

        return $filter('number')(bytes / (1024 * 1024), precision);
    };
}])
.filter('duration', function () {
    return function (d) { // duration in seconds
        //var ms = d % 1000;
        //d = (d - ms) / 1000;
        var s = d % 60;
        d = (d - s) / 60;
        var m = d % 60;
        d = (d - m) / 60;
        var h = d % 24;
        function pad2(i) { return (i > 9 ? '' : '0') + i; }
        return (h > 0 ? h + ':' + pad2(m) : m) + ':' + pad2(Math.round(s));
    };
})
.filter('secToHours', function () {
    return function (d) { // duration in seconds
        //var ms = d % 1000;
        //d = (d - ms) / 1000;
        var s = d % 60;
        d = (d - s) / 60;
        var m = d % 60;
        d = (d - m) / 60;
        var h = d;
        function pad2(i) { return (i > 9 ? '' : '0') + i; }
        return ( h + ':' + pad2(m) ) + ':' + pad2(Math.round(s));
    };
})
.filter('sToDuration', function () {
return function (d) { // duration in seconds
    //var ms = d % 1000;
    //d = (d - ms) / 1000;
    //d = d / 1000; //ms to s
    var s = d % 60;
    d = (d - s) / 60;
    var m = d % 60;
    d = (d - m) / 60;
    var h = d % 24;
    var days = (d - h) / 24;
    return (days > 0 ? days + ' days ' : '') + (days > 0 ? h + 'h ' + m : (h > 0 ? h + 'h ' + m : m)) + 'm ' + Math.floor(s) + 's';
};
})
.filter('multilineTooltip', function () {
return function (input) {
      return input.replace('<br />', '\n').replace('<br/>', '\n');
  };
})
.filter('skip', function () {
    return function (list, skipCount) {
        if (!list)
            return list;
        if (skipCount > 0)
            return list.slice(skipCount);
        else
            return list.slice(0, list.length + skipCount);
    };
})
.filter('take', function () {
    return function (list, takeCount) {
        if (!list)
            return list;
        if (takeCount < list.length)
            return list.slice(0, takeCount);
        else
            return list;
    };
})
.filter('noUnderscores', function () {
    return function (input) {
        return input.replace(/_/g, ' ');
    };
});
