function DailyClockController() {
	var ctrl = this;

	ctrl.clockCols = [0, 1, 2, 3];
	ctrl.clockRows = [0, 1, 2, 3, 4, 5];

	ctrl.click = function (hour) {
		ctrl.onClick({ hour: hour });
	}

	ctrl.dblClick = function (hour) {
		ctrl.onDblClick({ hour: hour });
	}

	ctrl.getFillClass = function (hour) {
		return ctrl.fillClass({ hour: hour });
	}
}

angular.module('app')
.component('wpDailyClock',
	{
		templateUrl: '/Modules/WebPlayer/dailyClockComponent.html',
		controller: DailyClockController,
		bindings: {
			onClick: '&',
			onDblClick: '&',
			disabled: '<',
			fillClass: '&'
		}
	});
