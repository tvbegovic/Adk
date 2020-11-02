'use strict';

angular.module('app')
	.factory('ValueFormatter', ['AppSettings', function (AppSettings) {
		var _local = null;
		AppSettings.getLocal().then(function (cultureName) { _local = cultureName; });

		// function to format a number with separators. returns formatted number.
		// num - the number to be formatted
		// decpoint - the decimal point character. if skipped, "." is used
		// sep - the separator character. if skipped, "," is used
		function formatNumberBy3(num, decpoint, sep) {
			num = parseFloat(num);
			// check for missing parameters and use defaults if so
			if (arguments.length == 2) {
				sep = ',';
			}
			if (arguments.length == 1) {
				sep = ',';
				decpoint = '.';
			}
			// need a string for operations
			num = num.toString();
			// separate the whole number and the fraction if possible
			var a = num.split('.'); //Js always use . for numerical
			var x = a[0]; // decimal
			var y = a[1]; // fraction
			var z = '';


			if (typeof (x) != 'undefined') {
				// reverse the digits. regexp works from left to right.
				for (var i = x.length - 1; i >= 0; i--) {
					z += x.charAt(i);
				}
				// add seperators. but undo the trailing one, if there
				z = z.replace(/(\d{3})/g, '$1' + sep);
				if (z.slice(-sep.length) == sep) {
					z = z.slice(0, -sep.length);
				}
				x = '';
				// reverse again to get back the number
				for (i = z.length - 1; i >= 0; i--) {
					x += z.charAt(i);
				}
				// add the fraction back in, if it was there
				if (typeof (y) != 'undefined' && y.length > 0) {
					x += decpoint + y;
				}
			}
			return x;
		}

		return {
			convertSecondsToHourFormat: function (seconds) {
				var hours = Math.floor(seconds / 3600);
				var minutes = Math.floor(seconds / 60);
				var ms = seconds * 1000;
				var format = 'H:mm:ss';

				if (minutes < 1) {
					format = 's';
				} else if (minutes < 10) {
					format = 'm:ss';
				} else if (hours < 1) {
					format = 'mm:ss';
				} else if (hours >= 24) {
					format = hours + ':mm:ss';
				}
				return ms == 0 ? '0' : moment('2000-01-01 00:00:00').add(moment.duration(ms)).format(format);
			},
			roundWithDecimalPlaces: function (num, decimalPlaces) {
				decimalPlaces = decimalPlaces || 0;
				var decimalPlacesFactor = Math.pow(10, decimalPlaces);
				return (Math.round(num * decimalPlacesFactor) / decimalPlacesFactor).toFixed(1);
			},
			toPercentageString: function (num) {
				return this.roundWithDecimalPlaces(num, 1) + '%';
			},
			roundServerNumberString: function (num) {
				return num && isNaN(num) ? Math.round(parseFloat(num.replace(',', '.'))) : num;
			},
			toLocalString: function (val, excludeDecimalPalces) {
				var num = parseFloat(val);
				if (isNaN(num)) { return val; }
				if (excludeDecimalPalces) { num = Math.round(num); }
				return formatNumberBy3(num, _local.numDecpoint, _local.numSeparator);
			},
			zeroToEmptyString: function (value) {
				if (value === 0 || value === '0') {
					return '';
				}
				return value;
			},
			getServerStringDateWithoutTime: function (localDate) {
				//js getMonth is zero based
				return localDate.getFullYear() + '-' + (localDate.getMonth() + 1) + '-' + localDate.getDate();
			},
			columnValueToSortIndicator: function (colA, colB, ascending) {
				//sort undefined to bottom of the list no matter if it's ascending or descending ordering.
				if (!colA) { return 1; }
				else if (!colB) { return -1; }

				else if (colA < colB) { return ascending ? -1 : 1; }
				else if (colA > colB) { return ascending ? 1 : -1; }
				return 0;
			},
			convertPieChartDataToDiscreteBarData: function (pieChartData, settings) {
				settings = settings || {};
				var dataToConvert = pieChartData;
				if (!pieChartData) {
					return pieChartData;
				}

				if (settings.sort) {
					dataToConvert = pieChartData.sort(function (a, b) {
						return b.Value - a.Value;
					});
				}

				return [{
					values: dataToConvert
				}];
			},
			convertServerLineChartData: function (serverLineChartData) {
				var data = [];
				for (var a in serverLineChartData) {
					var row = serverLineChartData[a];
					var lineData = [];
					for (var p in row.Values) {
						var point = row.Values[p];
						lineData.push({ x: moment(point.Key), y: point.Value });
					}
					data.push({
						values: lineData,
						key: row.Key
					});
				}
				return data;
			},
			//netDateIs in following format => /Date(1475272800000)/
			netDateToJsDate: function (netDate) {
				var re = /-?\d+/;
				var m = re.exec(netDate);
				return new Date(Number(m[0]));
			},
			truncateString: function (str, length, truncateStr) {
				truncateStr = truncateStr || '...';
				length = ~~length;
				return str.length > length ? str.slice(0, length) + truncateStr : str;
			}
		};
	}])
	//ADD FILTERS FOR VALUE FORMATTER SERVICE
	.filter('zeroToEmptyString', ['ValueFormatter', function (ValueFormatter) {
		return function (value) {
			return ValueFormatter.zeroToEmptyString(value);
		};
	}])
	.filter('toPercentageString', ['ValueFormatter', function (ValueFormatter) {
		return function (value) {
			var num = parseFloat(value);
			return isNaN(num) ? value : ValueFormatter.toPercentageString(num);
		};
	}])
	.filter('toLocalString', ['ValueFormatter', function (ValueFormatter) {
		return function (value) {
			var num = parseFloat(value);
			return isNaN(num) ? value : ValueFormatter.toLocalString(Math.round(num), true);
		};
	}])
	.filter('secondsToTimeFilter', ['ValueFormatter', function (ValueFormatter) {
		return function (value) {
			var num = parseFloat(value);
			return isNaN(num) ? value : ValueFormatter.convertSecondsToHourFormat(num);
		};
	}]);
