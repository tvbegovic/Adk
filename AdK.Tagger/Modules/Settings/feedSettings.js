angular.module('app')
	.controller('feedSettingsCtrl', ['$scope', 'Service', 'emailComposer', 'CachedApiCalls','htmlSettingService',
		function ($scope, Service, emailComposer, CachedApiCalls, htmlSettingService) {
		    $scope.showError = false;

		    Service('GetFeedSettings').then(function (feedSettings) {
                $scope.feedSettings = feedSettings;
                $scope.feedSettings.FeedProducts.forEach(function (p) {
                    p.DisplayName = p.Name;
                });
		    })

		    $scope.editFeedEmail = function () {

		        emailComposer.open(
					'Ad Alert Email', null,
					$scope.feedSettings.FeedMailSubject,
					$scope.feedSettings.FeedMailBody,
					['FeedList-START', 'playbackPageUrl', 'advertiserName', 'brandName', 'advertiserAndBrandName', 'media', 'title', 'duration', 'market', 'channels', 'FirstAiringsList-START', 'channelName', 'firstAiringTime', 'FirstAiringsList-END', 'adTranscript', 'FeedList-END', 'feedPageUrl']
				).then(function (template) {
				    $scope.feedSettings.FeedMailSubject = template.subject;
				    $scope.feedSettings.FeedMailBody = template.body;

				    $scope.saveSettings();
				});
		    }

			$scope.editAdFeedMessage = function () {
				
				htmlSettingService.open(
					'Ad feed emtpy message','Message',
					$scope.feedSettings.AdFeedEmptyMessage					
				).then(function (template) {
					$scope.feedSettings.AdFeedEmptyMessage = template.text;
					$scope.saveSettings();
				});
			}

		    $scope.saveSettings = function () {
		        Service('SaveFeedSettings', { settings: $scope.feedSettings })
					.catch(function () {
					    $scope.showError = true;
					});
            }

            $scope.getProducts = function (term) {
                return getFilteredCachedAutoCompleteSuggestions(term, $scope.keyChain.products);
            };

            $scope.keyChain = {
                products: 'products'
            };

            var lastAjaxTerms = {};
            $scope.loadingSwitches = {};
            var itemArrays = {};

            for (var key in $scope.keyChain) {
                lastAjaxTerms[$scope.keyChain[key]] = { term: '' };
                $scope.loadingSwitches[$scope.keyChain[key]] = { loading: false };
                itemArrays[$scope.keyChain[key]] = [];
            }

            CachedApiCalls.getAllProducts().then(function (response) {
                var key = $scope.keyChain.products;
                itemArrays[key] = [];
                for (var i = 0; i < response.length; i++)
                    itemArrays[key].push({ Id: response[i].Id, DisplayName: response[i].Name });
            });
			
            function getFilteredCachedAutoCompleteSuggestions(term, key) {
                return itemArrays[key].filter(function (item) {
                    return item.DisplayName.toLowerCase().indexOf(term.toLowerCase()) !== -1;
                });
            }

		}]).directive('feedSettings', [function () {
		    return {
		        restrict: 'E',
		        scope: {
		        },
		        templateUrl: '/Modules/Settings/feedSettings.html',
		        controller: 'feedSettingsCtrl'
		    };
		}]);
