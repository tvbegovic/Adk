function CalendarComponentController($scope, $element, $attrs) {
	var ctrl = this;

	ctrl.dayClass = function (date, mode) {
		return ctrl.getDayClass({ date: date, mode: mode });
	}

	ctrl.dateDisabled = function (date, mode) {
		return ctrl.getDateDisabled({ date: date, mode: mode });
	}

	ctrl.onDateChange = function () {
		ctrl.onChange({ date: ctrl.dt });
	}
}

angular.module('app')
	.component('wpCalendar',
	{
		templateUrl: '/Modules/WebPlayer/calendarComponent.html',
		bindings: {
			dt: '=',
			getDayClass: '&',
			onChange: '&',
			getDateDisabled: '&'

		},
		controller: CalendarComponentController
	});
