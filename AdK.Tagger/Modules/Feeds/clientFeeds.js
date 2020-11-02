angular.module('app')
.controller('clientFeedsCtrl', ['$scope', '$routeParams', '$location','cookieService',
	function ($scope, $routeParams, $location,cookieService) {
        $scope.displayTypeFeed = 0;
        if ($routeParams.returnTo != null)
            cookieService.put('FeedResults_returnTo', $routeParams.returnTo);

    $scope.displayTypeList = 1;

		if ($routeParams.id) {
			$scope.selectedFeed = {
					id: $routeParams.id,
					name: decodeURIComponent($routeParams.name)
			};
			$scope.displayType = $scope.displayTypeFeed;
		} else {
			$scope.displayType = $scope.displayTypeList;
		}

    $scope.showFeed = function (feed) {
			$location.path('client-feeds/view/' + feed.Id + '/' + encodeURIComponent(feed.Client) + '/clientFeeds');
    };

    /*$scope.showList = function () {
        var returnTo = $routeParams.returnTo;
        if (returnTo == null)
        {
            returnTo = cookieService.get('clientFeedResults_returnTo');            
        }            
        
        if (returnTo == 'userFeeds')
            $location.path('/add-feed/');
		else if(returnTo == null || returnTo == 'clientFeeds')
            $location.path('client-feeds/view/');
            
	};*/
    $scope.backButton = function () {
        $scope.returnTo = $routeParams.returnTo;
        if ($scope.returnTo == null) {
            $scope.returnTo = cookieService.get('FeedResults_returnTo');
        }
        //history.go(-1);
        if ($scope.returnTo) {

            if ($scope.returnTo == 'userFeeds')
                $location.path('/add-feed/');
            if ($scope.returnTo == 'reports')
                $location.path('client-feeds/reports');
            if ($scope.returnTo == 'userFeeds')
                $location.path('/add-feed/');
            if ($scope.returnTo == 'clientFeeds')
                $location.path('/client-feeds/view');
        }
        else
            $location.path('/client-feeds/view');
    };
} ]);

