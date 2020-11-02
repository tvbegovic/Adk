angular.module('app.reports')
	.service('AuditService', [function () {
		return {
			filter: {
				showPartials: false
			}
		};
	}]);

