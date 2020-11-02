angular.module('app')
	.run(['$rootScope', 'Service', function ($rootScope, Service) {

		localStorage.UserDateFormat = 'yyyy-MM-dd';
		Service('GetSettings').then(function (config) {
			if(config && config.Global && config.Global.UserDateFormat) {
				localStorage.UserDateFormat = config.Global.UserDateFormat;
			}
		});

		$rootScope.headerVisibleHeight = 98;
		angular.element(window).bind('scroll', observeHeader);

		function observeHeader() {
			var header = document.querySelector('#main-header');
			var headerHeight = header ? header.clientHeight : 0;

			var headerScrolledOut = window.pageYOffset > headerHeight;
			if ($rootScope.headerScrolledOut != headerScrolledOut) {
				$rootScope.$apply(function () {
					$rootScope.headerScrolledOut = headerScrolledOut;
					$rootScope.headerVisibleHeight = $rootScope.headerScrolledOut ? 0 : headerHeight - window.pageYOffset;
				});
			}
		}

		angular.element(document).bind('keyup', function (event) {
			if (event.which === 27) {
				$rootScope.$broadcast('escaped');
			}
		});


	}])
	.directive('autofocus', function () {
		return {
			restrict: 'A',
			link: function (scope, element) {
				element[0].focus();
			}
		};
	})
	.directive('focusOn', function () {
		return function (scope, elem, attr) {
			scope.$on('focusOn', function (e, name) {
				if (name === attr.focusOn) {
					elem[0].focus();
				}
			});
		};
	})
	.factory('focus', ['$rootScope', '$timeout', function ($rootScope, $timeout) {
		return function (name) {
			$timeout(function () {
				$rootScope.$broadcast('focusOn', name);
			});
		};
	}])
	.directive('onEnter', function () {
		return {
			restrict: 'A',
			scope: {
				onEnter: '&'
			},
			link: function (scope, element) {
				element.bind('keypress', function (event) {
					if (event.which === 13) {
						scope.$apply(function () {
							scope.onEnter();
						});
						event.preventDefault();
					}
				});
			}
		};
    })
	.directive("keepFocus", ['$timeout', function ($timeout) {
    /*
    Intended use:
        <input keep-focus ng-model='someModel.value'></input>
    */
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function ($scope, $element, attrs, ngModel) {

            ngModel.$parsers.unshift(function (value) {
                $timeout(function () {
                    $element[0].focus();
                });
                return value;
            });

        }
    };
	}])
.directive('modelChangeBlur', function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, elm, attr, ngModelCtrl) {
            if (attr.type === 'radio' || attr.type === 'checkbox') return;

            elm.unbind('input').unbind('keydown').unbind('change');
            elm.bind('blur', function () {
                scope.$apply(function () {
                    ngModelCtrl.$setViewValue(elm.val());
                });
            });
        }
    };
})
    ;

