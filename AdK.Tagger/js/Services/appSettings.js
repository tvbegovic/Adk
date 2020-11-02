angular.module('app')
  .factory('AppSettings', ['$q', '$timeout', 'Service', function ($q, $timeout, Service) {
      var _appSettings = {};
      var _reportsToHide = null;
      var _local = {
          cultureName: '',
          numDecpoint: ',',
          numSeparator: '.'
      };
      var settingsLoaded = false;

      Service('GetAppSettings').then(function (settings) {
          _appSettings = settings;
          setReportsToHide();
          var cultureName = _.find(settings, function (setting) { return setting.Key === 'CultureName'; });
          setLocal(cultureName)

          settingsLoaded = true;
      }).catch(function () {
          settingsLoaded = true;
      });

      function setReportsToHide() {
          if (_reportsToHide === null) {
              _reportsToHide = [];
              var reportsToHideSetting = _.find(_appSettings, function (setting) {
                  return setting.Key == 'ReportsToHide';
              });

              if (reportsToHideSetting && reportsToHideSetting.Value && reportsToHideSetting.Value.length) {
                  _reportsToHide = reportsToHideSetting.Value.split(',');
                  for (var i = 0; i < _reportsToHide.length; i++) {
                      _reportsToHide[i] = _reportsToHide[i].trim();
                  }
              }

          }
      }

      function setLocal(culture) {
          _local.cultureName = culture ? culture.Value || '' : '';
          switch (_local.cultureName.toLowerCase()) {
              case 'en-us':
              case 'en-tt':
              case 'en-gb':
              case 'en-uk':
              case 'chs':
              case 'fr':
              case 'fr-ch':
              case 'ja':
              case 'th':
                  _local.numDecpoint = '.';
                  _local.numSeparator = ',';
                  break;
              default:
                  _local.numDecpoint = ',';
                  _local.numSeparator = '.';
          }
      }

      function waitForSettings() {
          var deferred = $q.defer();
          resolveDefferedOnSettingLoaded(deferred);
          return deferred.promise;
      }

      function resolveDefferedOnSettingLoaded(deferred) {
          if (settingsLoaded) {
              deferred.resolve();
              return;
          }

          $timeout(function () {
              resolveDefferedOnSettingLoaded(deferred);
          }, 1);
      }

      return {
          getReportsToHide: function () {
              var deferred = $q.defer();
              waitForSettings().then(function () {
                  deferred.resolve(_reportsToHide);
              }).catch(function (exc) { deferred.reject(exc); });

              return deferred.promise;
          },
          getLocal: function () {
              var deferred = $q.defer();
              waitForSettings().then(function () {
                  deferred.resolve(_local);
              }).catch(function (exc) { deferred.reject(exc); });

              return deferred.promise;
          },
          getRawSettings: function () {
              return _appSettings;
          }
      };

  }]);
