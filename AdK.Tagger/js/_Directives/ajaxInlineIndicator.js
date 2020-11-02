angular.module('app')
  .directive('ajaxInlineIndicator', function() {
    return {
      restrict: 'AE',
      scope: {
        showSpinner: '=',
				showSuccess: '='
      },
      template: '<span style="margin-right: 2px; font-size: 18px;">' +
										'<i class="fa fa-spinner fa-spin ajax-spinner" ng-show="showSpinner"></i>' +
										'<i class="fa fa-check" style="color:green;" ng-show="showSuccess"></i>' +
								'</span>'
    };
  });


