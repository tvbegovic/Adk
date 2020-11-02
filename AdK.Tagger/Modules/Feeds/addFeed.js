angular.module('app')
    .controller('addFeedCtrl', ['$scope', 'Service', '$routeParams','$location', 'LocalStorage','cookieService', function ($scope, Service, $routeParams, $location, LocalStorage, cookieService) {
        $scope.feeds = [];
        $scope.reportId = $routeParams.reportId;
        if (!$scope.reportId)
        {
			Service('GetFeedsForCurrentUser').then(function (res) {
				if (res != null && res.length > 0)
				{
					res.forEach(function (item) {
						item.Timestamp = item.Timestamp ? moment(item.Timestamp).toDate() : null;
						item.LastEmailSent = item.LastEmailSent ? moment(item.LastEmailSent).toDate() : null;
					});
					$scope.feeds = res;
					LocalStorage.setJson('userFeeds', res);
				}
				else
				{
					Service('GetFeedSettings').then(function (res) {
						$scope.errorMessage = res.AdFeedEmptyMessage;
					});
				}				
                
            },
                function (response) {
                    $scope.errorMessage = response.data.Message;
                }
            );
        }

        $scope.backButton = $routeParams.reportId != null ? function () {
            $scope.returnTo = $routeParams.returnTo;
            if ($scope.returnTo == null) {
                $scope.returnTo = cookieService.get('FeedResults_returnTo');
            }
            //history.go(-1);
            if ($scope.returnTo) {

                if ($scope.returnTo == 'userFeeds')
                    $location.path('/add-feed/');
				if ($scope.returnTo == 'reports')
				{
					var id='', client='', cm='';
					var cached = JSON.parse(cookieService.get('userFeedReportsLastParams'));
					if (cached != null) {
						client = cached.clientName;
						id = cached.id;
						cm = cached.countryMarket;
					}
					$location.path(`add-feed/reports/${id}/${client}/${cm}`);					
					
				}
                    
                if ($scope.returnTo == 'userFeeds')
                    $location.path('/add-feed/');
                if ($scope.returnTo == 'reports_frompublic')
                {
					var id = '', client = '', cm = '';
					var cached = JSON.parse(cookieService.get('userFeedReportsLastParams'));
					if (cached != null) {
						client = cached.clientName;
						id = cached.id;
						cm = cached.countryMarket;
					}
					if (cm.length == 0)
						cm = '%20';
					location.href = `index.html#/add-feed/reports/${id}/${client}/${cm}`;
                }
                    
            }
        } : null;
    
}]);
