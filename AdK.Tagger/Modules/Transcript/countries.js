angular.module('app')
	.controller('transcriptCountriesCtrl',
	['$scope', '$filter', '$modal', '$routeParams', '$timeout', '$location', 'confirmPopup', 'Service',
		function ($scope, $filter, $modal, $routeParams, $timeout, $location, confirmPopup, Service) {

			initializing = true;

			function init() {
				$scope.gridDefinition = {
					columns: [
						{ field: 'Email', name: 'User', filter: '', hasFilter: true, filterTypeahead: false }
					],
					idField: 'Id',
					pager: false,
					fixedHeader: true,
					fixedClass: 'countriesGridFixed'
				};
				Service('GetUsersWithDomains').then(function (data) {
					$scope.domains = data.domains;
					$scope.users = data.usersWithDomains;
					GetUserCountryData(data);
				});
			}

			init();

			function GetUserCountryData(data) {
				initializing = true;
				$scope.users.forEach(function (u) {
					data.domains.forEach(function (d) {
						if (u.AssignedDomains != null)
							u[d.domain] = u.AssignedDomains.find(x => x.id == d.id) != null;
						else
							u[d.domain] = false;
						$scope.$watch(function () { return u[d.domain] }, function (newValue, oldValue) {
							if(newValue != oldValue)
								updateDomain(u, d, newValue);
						})
					})
				});
				data.domains.forEach(function (d) {
					$scope.gridDefinition.columns.push({ field: d.domain, name: d.domain_name, type: 'checkbox' });
				});
				initializing = false;
			}

			function updateDomain(user, domain, value) {
				if (!initializing) {
					Service('UpdateUserDomain', { userId: user.Id, domainId: domain.id, value: value }, { backgroundLoad: true }).then(function (data) {
					});
				}
			}
		}
	]);
